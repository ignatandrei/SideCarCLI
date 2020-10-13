echo "Exit Code was " %1
:Loop
IF "%1"=="0" GOTO Continue
   timeout 5
   echo "waiting"
GOTO Loop
:Continue
