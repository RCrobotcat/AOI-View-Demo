using PENet;
using AOIProtocol;
using UnityEngine;
using UnityEngine.UI;
using PEUtils;
using System.Collections.Concurrent;
using UnityEditor.PackageManager.UI;

public class GameRoot : MonoBehaviour
{
    public static GameRoot Instance;

    public Text entityIDTxt;
    public Button loginBtn;
    public Camera cam;
    public bool CamFollow = true; // 摄像机是否跟随主角

    public Transform entityRoot;
    public Transform cellRoot;

    AsyncNet<ClientSession, Package> client = new AsyncNet<ClientSession, Package>();
    ConcurrentQueue<Package> packageQueue = new ConcurrentQueue<Package>(); // 消息队列

    uint currentEntityID; // 当前实体ID

    void Start()
    {
        Instance = this;

        // 配置日志插件
        LogConfig config = new LogConfig
        {
            saveName = "AOIClientLog.txt",
            loggerEnum = LoggerType.Unity
        };
        PELog.InitSettings(config);

        client.StartAsClient("127.0.0.1", 18000);
    }

    void Update()
    {
        while (!packageQueue.IsEmpty)
        {
            if (packageQueue.TryDequeue(out Package pkg))
            {
                switch (pkg.cmd)
                {
                    case CMD.LoginResponse:
                        HandleLoginResponse(pkg.loginResponse);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                this.Error("Failed to dequeue package.");
            }
        }
    }

    /// <summary>
    /// 处理登录响应
    /// </summary>
    void HandleLoginResponse(LoginResponse response)
    {
        entityIDTxt.text = $"EntityID: {response.entityID}";
        currentEntityID = response.entityID;
    }

    public void AddMsgPackage(Package package)
    {
        packageQueue.Enqueue(package);
    }

    /// <summary>
    /// 点击登录按钮事件
    /// </summary>
    public void ClickLoginBtn()
    {
        client.session.SendMsg(new Package
        {
            cmd = CMD.LoginRequest,
            loginRequest = new LoginRequest
            {
                account = "testAcc"
            }
        });

        loginBtn.interactable = false;
    }
}
