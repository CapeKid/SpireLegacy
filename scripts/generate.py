import json
from pathlib import Path
from preflight import validate, ROOT

sheets=validate()
out=ROOT/'src/main/java/heir'
lines=['package heir;', 'import com.google.gson.*;', 'import java.util.*;', 'public final class Data {', 'public static final Gson GSON = new Gson();', 'public static final Map<String,LinkedHashMap<String,Row>> sheets = new LinkedHashMap<>();', 'public static final class Row { final JsonObject value; Row(String json) { value=GSON.fromJson(json,JsonObject.class); } public String s(String key){return value.get(key).getAsString();} public int i(String key){return value.get(key).getAsInt();} public float f(String key){return value.get(key).getAsFloat();} public boolean b(String key){return value.get(key).getAsBoolean();} public List<String> list(String key){List<String> r=new ArrayList<>();for(JsonElement e:value.getAsJsonArray(key))r.add(e.getAsString());return r;} }', 'static Row add(String sheet,String json){ Row r=new Row(json); if(!sheets.containsKey(sheet))sheets.put(sheet,new LinkedHashMap<String,Row>()); sheets.get(sheet).put(r.s("id"),r);return r;}', 'public static Row row(String sheet,String id){Row r=sheets.get(sheet).get(id);if(r==null)throw new IllegalArgumentException(sheet+":"+id);return r;}', 'public static Collection<Row> rows(String sheet){return sheets.get(sheet).values();}']
for name,rows in sheets.items():
    for row in rows:
        symbol=name.upper()+'_'+row['id'].upper()
        literal=json.dumps(json.dumps(row,separators=(',',':')))
        lines.append(f'public static final Row {symbol} = add("{name}",{literal});')
lines.append('}')
(out/'Data.java').write_text('\n'.join(lines)+'\n')
cards=out/'cards'; cards.mkdir(exist_ok=True)
for row in sheets['cards']:
    symbol=''.join(w.title() for w in row['id'].split('_'))
    (cards/(symbol+'.java')).write_text(f'package heir.cards; public final class {symbol} extends heir.HeirCard {{ public {symbol}(){{super("{row["id"]}");}} }}\n')
resources=ROOT/'src/main/resources/heir'; resources.mkdir(exist_ok=True)
(resources/'assets.json').write_text(json.dumps(sheets['assets'],indent=2))
(resources/'host_assets.json').write_text(json.dumps(sheets['host_assets'],indent=2))
print('Generated one Java row constant per sheet row and one class per card.')
