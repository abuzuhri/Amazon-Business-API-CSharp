"""
Fetch an OpenAPI spec from an Amazon Business docs model page.

Usage: python fetch_spec.py <model_page_slug> <output_filename>
Example: python fetch_spec.py reconciliation-api-v1-model Reconciliation_API.json

The Amazon Business docs are hosted on ReadMe.com. The spec is embedded as a
fenced ```json``` block inside the `doc.body` field of the page's `ssr-props`
hydration script — easier and more reliable than scraping the rendered HTML
(which is JS-hydrated) or going through a summarizer-LLM tool.

Post-processing: identical inline parameter enums (same `name` + `in` + sorted
`enum` values across multiple operations) are hoisted into `parameters/*` and
replaced with `$ref`s. NSwag then generates a single shared C# enum per param
definition instead of `RegionN` copies — much nicer caller surface.
"""
import json
import re
import sys
import urllib.request
from pathlib import Path

BASE_URL = "https://developer-docs.amazon.com/amazon-business/docs/"
OUTPUT_DIR = Path(__file__).parent.parent / "Source" / "CSharpAmazonBusinessAPI" / "OpenAPIs"


def fetch_spec(slug: str) -> dict:
    url = BASE_URL + slug
    req = urllib.request.Request(url, headers={
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36",
    })
    with urllib.request.urlopen(req) as resp:
        html = resp.read().decode("utf-8", errors="replace")

    m = re.search(r'<script id="ssr-props"[^>]*>(.*?)</script>', html, re.DOTALL)
    if not m:
        raise RuntimeError(f"ssr-props script not found at {url}")

    data = json.loads(m.group(1))
    body = data.get("doc", {}).get("body", "")
    if not body:
        raise RuntimeError(f"doc.body empty at {url}")

    # Body is markdown — extract the first ```json fenced block.
    fence = re.search(r"```json\s*\n(.*?)\n```", body, re.DOTALL)
    if not fence:
        raise RuntimeError(f"```json``` block not found in body at {url}")

    return json.loads(fence.group(1))


def _pascal(name: str) -> str:
    return "".join(part[:1].upper() + part[1:] for part in re.split(r"[_-]", name) if part)


def _rename_definition(spec: dict, old: str, new: str) -> None:
    """Rename definitions/{old} → definitions/{new}, rewriting every $ref in the spec."""
    if old not in spec.get("definitions", {}):
        return
    spec["definitions"][new] = spec["definitions"].pop(old)
    old_ref = f"#/definitions/{old}"
    new_ref = f"#/definitions/{new}"

    def walk(obj):
        if isinstance(obj, dict):
            for k, v in list(obj.items()):
                if k == "$ref" and v == old_ref:
                    obj[k] = new_ref
                else:
                    walk(v)
        elif isinstance(obj, list):
            for item in obj:
                walk(item)

    walk(spec)


def dedupe_inline_enums(spec: dict) -> int:
    """Hoist parameter enums that repeat across operations into parameters/* refs.

    Matches by (name, in, sorted enum values). The first occurrence's full param
    definition wins (preserves description / default / etc.). Returns the number
    of $refs written.
    """
    occurrences: dict[tuple, list[tuple]] = {}

    def visit(parameters: list, parent: list) -> None:
        for i, param in enumerate(parameters):
            if not isinstance(param, dict):
                continue
            if "$ref" in param or "enum" not in param or "name" not in param:
                continue
            key = (param["name"], param.get("in", ""), tuple(sorted(param["enum"])))
            occurrences.setdefault(key, []).append((parent, i))

    for path_obj in spec.get("paths", {}).values():
        if not isinstance(path_obj, dict):
            continue
        for method, op in path_obj.items():
            if not isinstance(op, dict):
                continue
            params = op.get("parameters")
            if isinstance(params, list):
                visit(params, params)

    spec.setdefault("parameters", {})
    refs_written = 0
    used_names: set[str] = set(spec["parameters"].keys()) | set(spec.get("definitions", {}).keys())

    for (name, loc, values), locs in occurrences.items():
        if len(locs) < 2:
            continue  # only one occurrence — leave inline

        preferred = _pascal(name)

        # NSwag derives the C# enum type name from the parameter's `name` field, not from
        # the parameters/{key} we hoist into. So if there's already a definitions/{Name}
        # holding the same values, NSwag would emit `Name` for one and `Name2` for the
        # other. Detect that case and rename the existing schema definition out of the way
        # so the parameter wins the clean name. Same-values check guarantees we only
        # rename when the schemas are semantically identical.
        existing_def = spec.get("definitions", {}).get(preferred)
        if (
            existing_def
            and isinstance(existing_def, dict)
            and "enum" in existing_def
            and tuple(sorted(existing_def["enum"])) == values
        ):
            renamed = f"{preferred}Code"
            if renamed not in used_names:
                _rename_definition(spec, preferred, renamed)
                used_names.discard(preferred)
                used_names.add(renamed)

        # Pick a definition name. First-choice is the bare PascalCase param name; on
        # remaining collisions, suffix with location ("Query"/"Path"); last resort,
        # append "Parameter".
        candidates = [preferred, f"{preferred}{_pascal(loc)}", f"{preferred}Parameter"]
        def_name = next((c for c in candidates if c not in used_names), None)
        if def_name is None:
            continue  # give up rather than overwrite
        used_names.add(def_name)

        # Take the first occurrence verbatim as the canonical definition.
        canonical_parent, canonical_idx = locs[0]
        spec["parameters"][def_name] = canonical_parent[canonical_idx]

        # Replace every occurrence (including the first) with the same $ref.
        for parent, idx in locs:
            parent[idx] = {"$ref": f"#/parameters/{def_name}"}
            refs_written += 1

    return refs_written


def main() -> int:
    if len(sys.argv) != 3:
        print("Usage: python fetch_spec.py <model_page_slug> <output_filename>", file=sys.stderr)
        return 2

    slug, filename = sys.argv[1], sys.argv[2]
    spec = fetch_spec(slug)
    refs = dedupe_inline_enums(spec)
    spec_text = json.dumps(spec, indent=2)
    out_path = OUTPUT_DIR / filename
    out_path.write_text(spec_text, encoding="utf-8")
    print(f"Wrote {out_path} ({len(spec_text)} bytes, {refs} enum refs deduped)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
