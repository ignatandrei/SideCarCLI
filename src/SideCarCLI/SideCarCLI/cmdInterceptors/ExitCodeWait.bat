echo "Exit Code was " %1
:Loop
IF "%1"=="1" GOTO Continue
   echo "Exit Code was " %1
   timeout 50
   echo "waiting"
GOTO Loop
:Continue
