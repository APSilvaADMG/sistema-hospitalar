import re
from pathlib import Path

path = Path(r"C:\Users\Anderson\Projects\sistema-hospitalar\src\SistemaHospitalar.Infrastructure\Services\ReportsService.cs")
text = path.read_text(encoding="utf-8")

# Replace FormatDate calls inside EF Select blocks by marking for client-side - 
# simpler: inject AsEnumerable before dictionary selects won't work in async chain.

# Strategy: replace `.Select(X => new Dictionary<string, object?>` blocks ending before `.ToListAsync`
# with client-side mapping - too complex for regex.

# Instead add using and helper at top, fix known patterns with multiline regex for GroupBy dict selects
pattern = re.compile(
    r"(\s+)var rows = await ([^\n]+)\n(\s+)\.Select\(g => new Dictionary<string, object\?>\n(\s+)\{\n(\s+)\[\"(\w+)\"\] = ([^\n]+),\n(\s+)\[\"(\w+)\"\] = ([^\n]+),\n(\s+)\}\)\n(\s+)\.ToListAsync\(ct\);",
    re.MULTILINE,
)

def repl_group(m):
    indent = m.group(1)
    query = m.group(2)
    i2 = m.group(3)
    k1, v1, k2, v2 = m.group(6), m.group(7), m.group(8), m.group(9)
    i_end = m.group(11)
    return f"""{indent}var grouped = await {query}
{i2}.Select(g => new {{ Key = g.Key, Count = g.Count() }})
{i_end}.ToListAsync(ct);
{indent}var rows = grouped.Select(g => ReportRowBuilder.Row(
{indent}    ("{k1}", {v1.replace('g.Key', 'g.Key').replace('g.Key.ToString("dd/MM/yyyy")', 'g.Key.ToString("dd/MM/yyyy")')} if False else g.Key if "{k1}"=="never" else None),
)).ToList();"""

# Manual approach - just add using ReportRowBuilder and fix file by replacing all 
# `.Select(x => new Dictionary<string, object?>` with `.AsEnumerable().Select` - won't work in IQueryable

print("File size", len(text))
print("Dict selects", len(re.findall(r"Select\([^)]*=> new Dictionary", text)))
