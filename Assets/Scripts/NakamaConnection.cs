using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using UnityEngine.TextCore.Text;


public class NakamaConnection : MonoBehaviour
{
    //basic UI 
    [Header("Start Panel")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button enterChatButton;
    [SerializeField] private Button cancelChatButton;
    [Header("Lobby Panel")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Transform userLobbyContent;
    [SerializeField] private UserInfoItem UserInfoItemPrefab;
    [SerializeField] private Button cancelLobbyButton;
    
    private IDictionary<string, GameObject> players; 


    //connection points
    private string scheme = "http";
    private string host = "localhost";
    //private string hostPhone = "192.168.1.36";
    private int port = 7350;
    private string serverKey = "defaultkey";
    //private List<UserInfoItem> joinedUsers = new List<UserInfoItem>();
    //vars
    private IClient client;
    private ISession session;
    private ISocket socket;
    private IUserPresence localUser;
    private string ticket;
    private IMatchmakerTicket matchmakerTicket;
    private IMatch currentMatch;




    private void Start()
    {
        AddListeners();
        players = new Dictionary<string, GameObject>();
        cancelChatButton.gameObject.SetActive(false);
        enterChatButton.gameObject.SetActive(true);
        startPanel.SetActive(true);
        lobbyPanel.SetActive(false); ;
    }

    private void OnDestroy()
    {
        RemoveAllListners();
    }
    private void AddListeners() {
        enterChatButton.onClick.AddListener(Authenticate);
        cancelChatButton.onClick.AddListener(CancelMatchmaking);
        cancelLobbyButton.onClick.AddListener(CancelMatchmaking);
    }
    private void RemoveAllListners() {
        enterChatButton.onClick.RemoveAllListeners();
        cancelChatButton.onClick.RemoveAllListeners();
        cancelLobbyButton.onClick.RemoveAllListeners();
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

        CreateClient(username);
    }

    private void CreateClient(string username) {
        //create a client - with connection related info ss
        client = new Client(scheme, host, port, serverKey, UnityWebRequestAdapter.Instance);
        messageText.color = ColorConfig.normalColor;
        messageText.text = "Getting inside the matrix...";
        CreateSession(username);
    }

    public async void CreateSession(string username) {
        //some checks for device ids - just to make sure we dont gert unsupported id
        string id = SystemInfo.deviceUniqueIdentifier;
        //if (id == SystemInfo.unsupportedIdentifier)
        //{ 
        //generate new id
        id = Guid.NewGuid().ToString();
        //}
        session = await client.AuthenticateDeviceAsync(id, username);
        Debug.Log(string.Format("[Authentication Success] Session Id : {0} ,Username : {1}", session.UserId, session.Username));
        CreateSocket();
    }



    public async void CreateSocket() {
        //create socket connection
        socket = client.NewSocket(true);
        await socket.ConnectAsync(session, true);

        socket.ReceivedMatchmakerMatched += OnRecieveMatchmakerMatched;
        socket.ReceivedMatchPresence += OnReceivedMatchPresence;
        Debug.Log(string.Format("[Socket Connected] Connection Status : {0}", socket.IsConnected));

        FindMatch();
    }

    public async void FindMatch() {
        messageText.color = ColorConfig.normalColor;
        messageText.text =  "Finding Match...";


        //get a matchmking ticket - used to join match/cancel match
        matchmakerTicket = await socket.AddMatchmakerAsync("*",2,2, null, null);
        ticket = matchmakerTicket.Ticket;

        cancelChatButton.gameObject.SetActive(true);
        enterChatButton.gameObject.SetActive(false);
    }

    //cancel match making - removed player from match making queue
    public async void CancelMatchmaking() {
        await socket.RemoveMatchmakerAsync(matchmakerTicket);
        cancelChatButton.gameObject.SetActive(false);
        enterChatButton.gameObject.SetActive(true);
    }

    public async void OnRecieveMatchmakerMatched(IMatchmakerMatched matchMakerMatched) {
        localUser = matchMakerMatched.Self.Presence;
        
        
        //we found a match
        messageText.color = ColorConfig.normalColor;
        messageText.text = "Found Match...";

        //join the match
        var match = await socket.JoinMatchAsync(matchMakerMatched);
        currentMatch = match;
        startPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        
        foreach (var user in match.Presences) {
            SpawnPlayer(user);
        }
    }

    private void SpawnPlayer(IUserPresence user) {
        if (players.ContainsKey(user.SessionId))
        {
            return;
        }

        Debug.Log("User in presenses : " + user.Username);
        UserInfoItem item = Instantiate(UserInfoItemPrefab);
        item.transform.SetParent(userLobbyContent);
        item.transform.localScale = Vector3.one;
        item.Init(user.Username, user.UserId);

        players.Add(user.SessionId, item.gameObject);
    }

    private void DespawnPlayer(IUserPresence user) {
        if (!players.ContainsKey(user.SessionId)) {
            return;
        }

        Destroy(players[user.SessionId]);
        players.Remove(user.SessionId);
    }

    private void OnReceivedMatchPresence(IMatchPresenceEvent matchPresence)
    {
        //for each new join
        foreach (var newUser in matchPresence.Joins)
        {
            SpawnPlayer(newUser);
        }

        //for each that left
        foreach (var leftUser in matchPresence.Leaves) {
            DespawnPlayer(leftUser);
        }
    }

}
