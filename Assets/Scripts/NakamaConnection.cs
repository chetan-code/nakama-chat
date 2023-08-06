using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class NakamaConnection : MonoBehaviour
{
    //basic UI 
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private Button enterChatButton;
    [SerializeField] private TextMeshProUGUI messageText;

    //connection points
    private string scheme = "http";
    private string host = "localhost";
    private int port = 7350;
    private string serverKey = "defaultkey";
    //vars
    private IClient client;
    private ISession session;
    private ISocket socket;
    private string ticket;




    private void Start()
    {
        AddListeners();    
    }

    private void OnDestroy()
    {
        RemoveAllListners();
    }
    private void AddListeners() {
        enterChatButton.onClick.AddListener(Authenticate);
    }
    private void RemoveAllListners() {
        enterChatButton.onClick.RemoveAllListeners();
    }
    
    private async void Authenticate() {

        string username = usernameInputField.text;
        //check if username is valid
        if (username.Length < 2)
        {
            messageText.color = ColorConfig.errorColor;
            messageText.text = "Usename can't be empty or less than 2 characters";
            return;
        }

        CreateClient();
        CreateSession(username);
        CreateSocket();
    }

    private void CreateClient() {
        //create a client - with connection related info 
        client = new Client(scheme, host, port, serverKey, UnityWebRequestAdapter.Instance);
        messageText.color = ColorConfig.normalColor;
        messageText.text = "Getting inside the matrix...";
    }

    public async void CreateSession(string username) {
        //some checks for device ids - just to make sure we dont gert unsupported id
        string id = SystemInfo.deviceUniqueIdentifier;
        if (id == SystemInfo.unsupportedIdentifier)
        {
            //generate new id
            id = System.Guid.NewGuid().ToString();
        }
        session = await client.AuthenticateDeviceAsync(SystemInfo.deviceUniqueIdentifier, username);
        Debug.Log(string.Format("[Authentication Success] Session Id : {0} ,Username : {1}", session.UserId, session.Username));
    }

    public async void CreateSocket() {
        //create socket connection
        socket = client.NewSocket();
        await socket.ConnectAsync(session, true);

        socket.ReceivedMatchmakerMatched += OnRecieveMatchmakerMatched;
        Debug.Log(string.Format("[Socket Connected] Connection Status : {0}", socket.IsConnected));
    }

    public async void FindMatch() {
        messageText.color = ColorConfig.normalColor;
        messageText.text =  "Finding Match...";

        //get a matchmking ticket - used to join match/cancel match
        var matchMackingTicket = await socket.AddMatchmakerAsync("*",2,2);
        ticket = matchMackingTicket.Ticket;
    }

    public async void OnRecieveMatchmakerMatched(IMatchmakerMatched matchMakerMatched) {
        //we found a match

        //join the match
        var match = await socket.JoinMatchAsync(matchMakerMatched);
    }
}
