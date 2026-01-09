using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class Sonar : MonoBehaviour
{
    //https://github.com/Mark7888/steelseries-sonar-py/blob/master/src/steelseries_sonar_py/sonar.py#L93


    [Header("Is sonar setup ready")]
    public bool isReady;

    string volumePath = "/VolumeSettings/classic";
    string appDataPath;


    string encryptedAddress;
    string sonarAppAddress;

    [Header("Sliders values")]
    [Range(0, 1)]
    public float masterVolume;
    private float lastMasterVolume;
    [Range(0, 1)]
    public float gameVolume;
    private float lastGameVolume;
    [Range(0, 1)]
    public float chatRenderVolume;
    private float lastChatVolume;
    [Range(0, 1)]
    public float mediaVolume;
    private float lastMediaVolume;
    [Range(0, 1)]
    public float auxVolume;
    private float lastAuxVolume;

    public bool masterMuted;
    public bool gameMuted;
    public bool chatMuted;
    public bool mediaMuted;
    public bool auxMuted;

    public float sendRate;
    float sendingTimer;
    bool readyToSend;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isReady = false;

        appDataPath = "C:/ProgramData/SteelSeries/SteelSeries Engine 3/coreProps.json";

        StartCoroutine(SetupSonar());
    }

    private void Update()
    {
        if (!isReady) return;

        //MASTER CHANNEL
        if(lastMasterVolume != masterVolume)
        {
            if(readyToSend)
            {
                lastMasterVolume = masterVolume;
                StartCoroutine(SendSonarChannelValues(channelNames.master, masterVolume));
                ResetTimer();
            }
        }
        //GAME CHANNEL
        if (lastGameVolume != gameVolume)
        {
            if (readyToSend)
            {
                lastGameVolume = gameVolume;
                StartCoroutine(SendSonarChannelValues(channelNames.game, gameVolume));
                ResetTimer();
            }
        }
        //CHAT CHANNEL
        if (lastChatVolume != chatRenderVolume)
        {
            if (readyToSend)
            {
                lastChatVolume = chatRenderVolume;
                StartCoroutine(SendSonarChannelValues(channelNames.chatRender, chatRenderVolume));
                ResetTimer();
            }
        }
        //MEDIA CHANNEL
        if (lastMediaVolume != mediaVolume)
        {
            if (readyToSend)
            {
                lastMediaVolume = mediaVolume;
                StartCoroutine(SendSonarChannelValues(channelNames.media, mediaVolume));
                ResetTimer();
            }
        }
        //AUX CHANNEL
        if (lastAuxVolume != auxVolume)
        {
            if (readyToSend)
            {
                lastAuxVolume = auxVolume;
                StartCoroutine(SendSonarChannelValues(channelNames.aux, auxVolume));
                ResetTimer();
            }
        }

        if (sendingTimer <= 0)
        {
            readyToSend = true;
        }
        else
        {
            readyToSend = false;
            sendingTimer -= Time.deltaTime;
        }
    }

    IEnumerator SetupSonar()
    {
        
        //Start by getting the encrypted address from the config Json

        StreamReader reader = new StreamReader(appDataPath);
        string text = reader.ReadToEnd();
        JObject encryptedObject = JObject.Parse(text);

        encryptedAddress = (string)encryptedObject["ggEncryptedAddress"];

        //Now that we have encrypted address we get the app address

        string response = "";

        using (UnityWebRequest webRequest = UnityWebRequest.Get("https://" + encryptedAddress + "/subApps"))
        {
            webRequest.certificateHandler = new CustomCertificateHandler();
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    response = webRequest.error;
                    NotificationManager.Instance.QueueNotification(NotificationType.error, response);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    response = webRequest.error;
                    NotificationManager.Instance.QueueNotification(NotificationType.error, response);
                    break;
                case UnityWebRequest.Result.Success:
                    response = webRequest.downloadHandler.text;
                    NotificationManager.Instance.QueueNotification(NotificationType.info, "Succesfully connected to Sonar API");
                    break;
            }
        }

        JObject appObject = JObject.Parse(response);

        sonarAppAddress = (string)appObject["subApps"]["sonar"]["metadata"]["webServerAddress"];

        isReady = true;
    }

    IEnumerator SendSonarChannelValues(channelNames channel, float volume)
    {
        byte[] myData = System.Text.Encoding.UTF8.GetBytes("");

        string sendAddress = sonarAppAddress + volumePath + "/" + channel.ToString() + "/Volume/" + volume.ToString(); 
        Debug.Log(sendAddress);

        using (UnityWebRequest www = UnityWebRequest.Put(sendAddress, myData))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                NotificationManager.Instance.QueueNotification(NotificationType.error, "Error sending channel values");
            }
            else
            {
                Debug.Log("Upload complete!");
            }
        }
    }

    IEnumerator SendSonarMuteUnmuteChannel(channelNames channel, bool mute)
    {
        byte[] myData = System.Text.Encoding.UTF8.GetBytes("");

        string sendAddress = sonarAppAddress + volumePath + "/" + channel.ToString() + "/Mute/" + (mute ? "false" : "true");
        Debug.Log(sendAddress);

        using (UnityWebRequest www = UnityWebRequest.Put(sendAddress, myData))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Upload complete!");
            }
        }
    }

    IEnumerator FetchSonarValues()
    {
        string response = "";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(sonarAppAddress + volumePath))
        {
            webRequest.certificateHandler = new CustomCertificateHandler();
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    response = webRequest.error;
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    response = webRequest.error;
                    break;
                case UnityWebRequest.Result.Success:
                    response = webRequest.downloadHandler.text;
                    break;
            }
        }

        JObject jObject = JObject.Parse(response);

        masterVolume = float.Parse((string)jObject["masters"]["classic"]["volume"]);
    }


    public void MuteUnmuteSonarChannel(channelNames channel, bool unmuted)
    {
        StartCoroutine(SendSonarMuteUnmuteChannel(channel, unmuted));
    }

    private void ResetTimer()
    {
        sendingTimer = sendRate;
    }
}

public enum channelNames
{
    master,
    game,
    chatRender,
    media,
    aux,
    chatCapture
}
