import sqlite3
c = sqlite3.connect(r"file:G:/AAchina/Server/game/db/compact.sqlite3?mode=ro", uri=True)
c.row_factory = sqlite3.Row
print(dict(c.execute("SELECT id,name,stealth,detect_stealth,pacifist FROM buffs WHERE id=21583").fetchone()))
