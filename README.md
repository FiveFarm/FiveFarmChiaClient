# FiveFarmChiaClient
1. FiveFarm (https://FiveFarm.app) is a web based dashboard that you can use to view consolidated information for your farmers / harvesters. 
2. FiveFarm Chia client collects information from your farm and sends to server for displaying stats. FiveFarm Chia client is available for Windows and Linux.

# Solution Information
1. Solution is compatible with Visual Studio 2019 (16.11.2)
2. ChiaClientUI is an ASP.Net Project, ElectronNet Api is embeded into it to make it a desktop app
3. Use cmd to execute electron commands.
4. Chia.DB contains Database classes
5. Chia.Net is to communicate with RPC interfaces of chia
6. Chia.Common contains common settings
7. KeyCloak API is to login using keycloak
8. Install Wix Toolset to compile CustomActions Project and it is used in Advanced Installer Setup
9. CustomActionForms Project is used to show Message Boxes in Custom Action

# How to Make Build/Installer for Window or Linux/Ubuntu ?
1. For Windows: Just Run ./publish.bat Command in Developer PowerShell of Visual Studio. On Successful build, .exe Installer will be created.
2. For Linux/Ubuntu: Just Run ./publish_linux.bat Command in Terminal of Visual Studio Code. On Successful build, .deb Installer will be created.

# How to Run Fivefarm App for Test in Visual Studio / Visual Studio Code ?
Electronize Start. (please install npm before running command) 

# How to Setup/Install FiveFarm at your Machine ?
1. Download and Install FiveFarm Desktop client (Linux / Windows) from https://FiveFarm.app
2. Click on Login Button at https://FiveFarm.app. Here link for Signup will be displayed under login form. Create your account.
3. Now Run FiveFarm Desktop Client and Login with same credentials as were used in Step # 2.

# Note: 
1. Enter Passphrase if has been set in Chia Blockchain software, otherwise leave empty.
2. Passphrase is used only for reading status from Chia Blockchain. As it can be verified from this git repository.
