PATH=$PATH:$(readlink -f ../pyRevitManager/bin/Debug)
echo "[    ] Testing pyrevit version:" $(pyrevit -V)
echo "[    ] opening help page..." &
pyrevit help &
echo "[    ] opening blog page..." &
pyrevit blog &
echo "[    ] opening docs page..." &
pyrevit docs &
echo "[    ] opening source page..." &
pyrevit source &
echo "[    ] opening youtube page..." &
pyrevit youtube &
echo "[    ] opening support page..." &
pyrevit support
echo "[ OK ]"
echo "[    ] generating env report..."
pyrevit env
echo "[ OK ]"
echo "[    ] current clones are:"
pyrevit clones
echo "[ OK ]"
echo "[    ] current attachments are:"
pyrevit attached
echo "[ OK ]"
echo "[    ] installed extensions are:"
pyrevit extensions
echo "[ OK ]"
echo "[    ] extension lookup sources are:"
pyrevit extensions sources
echo "[ OK ]"
echo "[    ] installed and Running Revits are:"
pyrevit revits --installed
pyrevit revits
echo "[ OK ]"
echo "[    ] preparing temp test directory..."
mkdir -p /c/tests
cd /c/tests
echo "[    ] testing default cloning..."
pyrevit clone defaulttestmaster ./defaulttestmaster
cd defaulttestmaster
echo "[    ] git valid? " $(git rev-parse --is-inside-work-tree)
echo "[    ] git branch? " $(pyrevit clones branch defaulttestmaster)
echo "[ OK ]"
echo "[    ] testing default cloning with branch..."
cd ..
pyrevit clone defaultestdevelop ./defaultestdevelop --branch=develop
cd defaultestdevelop
echo "[    ] git valid? " $(git rev-parse --is-inside-work-tree)
echo "[    ] git branch? " $(pyrevit clones branch defaultestdevelop)
echo "[ OK ]"
echo "[    ] testing invalid and direct repo cloning..."
cd ..
pyrevit clone repotestmaster https://github.com/eirannejad/rsparam.git ./repotestmaster
echo "[ OK ]"
echo "[    ] finished testing cloning..."
cd /c/
echo "[    ] current clones are:"
pyrevit clones
echo "[    ] testing clone info..."
pyrevit clones info defaulttestmaster
pyrevit clones open defaulttestmaster
echo "[    ] testing clone registering..."
echo "[    ] testing clone registering: forgetting all..."
pyrevit clones forget --all
echo "[ OK ]"
echo "[    ] testing clone registering: adding..."
pyrevit clones add defaulttestmaster /c/tests/defaulttestmaster
echo "[ OK ]"
echo "[    ] testing clone registering: renaming..."
pyrevit clones rename defaulttestmaster defaultestmaster_new
echo "[ OK ]"
echo "[    ] testing clone registering: changing branch..."
pyrevit clones branch defaultestmaster_new
pyrevit clones branch defaultestmaster_new develop
echo "[    ] git branch?" $(pyrevit clones branch defaultestmaster_new)
pyrevit clones branch defaultestmaster_new master
echo "[    ] git branch?" $(pyrevit clones branch defaultestmaster_new)
# pyrevit clones version <clone_name> [<tag_name>]
echo "[ OK ]"
echo "[    ] testing clone registering: changing commit..."
pyrevit clones commit defaultestmaster_new
pyrevit clones commit defaultestmaster_new 96474e68316fd2ff3b792b550ffafc9e69fa5aa9
pyrevit clones rename defaultestmaster_new defaulttestmaster
echo "[    ] git commit?" $(pyrevit clones commit defaultestmaster_new)
echo "[    ] testing attaching..."
pyrevit attach defaulttestmaster latest --all
pyrevit attached
pyrevit detach --all
pyrevit attach defaulttestmaster latest --all --allusers
pyrevit attached
pyrevit detach --all
echo "[    ] removing clones..."
pyrevit clones delete --all --test
echo "[    ] cleaning up..."
rm -rf /c/tests