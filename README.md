# HiSuiteLauncher
A Launcher for HiSuite which grabs its ssl calls and hooks them

1. What does it do?

It will launch an instance of HiSuite, Huawei Smartphone PC Manager, if installed. This instance will have a hook to ssl calls made by HiSuite to HiCloud. So we can now handle responses by editing a simple text file for a full ota update. It won't touch anything else. With an update response, HiSuite should now handle the update process including the rollback option if available and a proper imei authorization to HiCloud. This is a way to force an update to your Huawei/Honor smartphone with a standard tool.

2. Setup and Launch

You will need FirmwareFinder (TeamMT). If you've been around Huawei/Honor forums you should already know what it is. With FF we can track ota updates and even run an authorization test for an update. This is important because we're gonna be using the ota ID and its full link to pass to HiSuite. If you spot an update and your device is not yet authorized to install it your phone will most likely avoid it either.

!IMPORTANT!: I've been using this method since march/19 without any problems. No wipes, connectivity losses, call problems, weird behavior whatsoever. In fact, I'm running EMUI 9.1 now. I don't know how you can ensure the ID you picked is the right one for your device - considering you are using the search correctly: for my Honor 10 C636 I search for COL-L29C636. You might notice the name of the device seems a bit strange and here comes a massive confusion. Col-L29 is now COL-LGRP2-OVS (C636 is overseas) with a bunch of small step rom versions like .226 .227. 228 .229. I usually pick the very first version my phone is authorized for. So my last update was .227. After the update I tested my phone for others versions like .228 and .229 and my phone was authorized for those too. Go figure!

Since HiSuite 9+ the cust info is in the update request and I believe HiCloud suggests the right package for your phone. The reason behind this is because the ota file I get sometimes it's different in size and checksum from the one you get from FF even if it's the same ID. Total speculation here.

ALWAYS take note of the rollback ID suggested by HiCloud. It might look like it's just a regular version but it's not! An update and a rollback IDs are different, with different flash scripts. HiCloud will send rollback data with major updates like from EMUI 8.1 to EMUI 9; EMUI 9 to EMUI 9.1. You can spot both IDs in FF as well.

Just extract the zip folder to any suitable folder in your computer and run the Launcher.exe. A console window will popup and HiSuite will appear right after. You can monitor HiSuite calls in the console window.

3. Instructions:

Remove root and TWRP. You have to be full stock.

Backup data. Even HiSuite will ask you to do it.

Be prepared to download at least 4Gb of data while keeping your phone plugged to your pc.

You will need a very basic understanding of json data structure.

You have 2 files that should remain in the main folder of the launcher:
- hisuite9_request_update.txt: you should add the ID to "versionID" and full link to the update ota in "url". Get those from FF.
- Please notice that there is a pattern here. The "url" data doesn't include the actual file.
- IMEI.txt: add the first IMEI of your phone to the first line of this file.

Hit Update in HiSuite and you should see the red dot indicating an ota update (in fact, the update you added in the file above).

- log.txt: this file holds the requests/responses during the current session.

- Launcher.exe.config: here you can find a few setup options like hisuite folder.

4. Bugs

- Well, as far as I'm concerned you shouldn't face any bugs since it's pretty straight forward. BUT, please take my advice: if you're not sure about which update to go for avoid it altogether.

- It does not work with incremental updates. The recovery will reject it.

- In your phone, about phone page, it might not show the update changelog. Sometimes it does show, sometimes it doesn't.

- I cannot confirm this but since I always update my phone through this method I don't get incremental updates anymore. I don't know if it's because I'm always way ahead (updates usually take forever to reach my phone before and that's why I've been doing all of this) or anything else.

5. Notes:

- I have to thank Smaehtin (XDA) for kindly answering my request to support this tool for Honor 10 and for actually providing this method.
- Although I'm a professional developer (ERP stuff), I'm not used to C# so the code might look a bit messy and funky here and there. I'm sorry for that.
- The solution used is from a freeware compiler, SharDevelop (http://www.icsharpcode.net/OpenSource/SD/).
- Disclaimer here, I am not responsible for any damage you (your phone) might suffer from attempting this. I tested it myself several times in my phone, Honor 10 C636 (COL-L29C636). HiSuite will handle the whole update process but you still can provide wrong data as of wrong IDs or wrong url.
- We are using EasyHook (https://easyhook.github.io/) and Newtonsoft.Json (https://www.newtonsoft.com/json) libs.
