# How to Install and run Ollama
This guide explains how to get started with an offline LLM using Ollama Software

## Install (Windows)
- Go to https://ollama.com
- Click the Download button top right and choose Windows (download is ~800 MB)
- Run the installer (just press 'Install') [will take rougly 5gb of space + the space for the models you download]
- Ollama should start automatically (you will see a small lama icon in the notification area when it is runnning)
- Now go back to the website, Click 'Models' in the top Left section and choose the model you wish to try
  - You can click on a model and in its variant dropdown see how big it is. In general you need to choose one that is smaller than the amount of memory you have on your machine
  - Next to the dropdown is the command you need to run
- Once you have decided on your model copy the command and run it in a Windows Terminal
  - Example I choose llama3.2 3b model and write `ollama run llama3.2:3b`  
- The first time you run the model it will download if not already downloaded
- Once ready you now have a Terminal with a prompt you can try the model out + you can program against it (on URI http://127.0.0.1:11434)