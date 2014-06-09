KOSDLL="../../../../../Program Files (x86)/Steam/steamapps/common/Kerbal Space Program/GameData/kOS/Plugins/kOS.dll"
BUILDDLL=src/bin/Debug/kOS.dll

cp "$BUILDDLL" "$KOSDLL"
ls -l "$BUILDDLL" "$KOSDLL"
