using PENet;
using AOIProtocol;
using UnityEngine;
using UnityEngine.UI;
using PEUtils;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class GameRoot : MonoBehaviour
{
    public static GameRoot Instance;

    public Text entityIDTxt;
    public Button loginBtn;
    public Camera cam;
    public bool CamFollow = true; // ������Ƿ��������

    public Transform entityRoot;
    public Transform cellRoot;

    AsyncNet<ClientSession, Package> client = new AsyncNet<ClientSession, Package>();
    ConcurrentQueue<Package> packageQueue = new ConcurrentQueue<Package>(); // ��Ϣ����

    Dictionary<uint, GameObject> playerDic = null; // ����ֵ�(ʵ��ID, ��Ҷ���)

    uint currentEntityID; // ��ǰʵ��ID
    GameObject currentPlayerGo; // ��ǰ��Ҷ���

    void Start()
    {
        Instance = this;

        // ������־���
        LogConfig config = new LogConfig
        {
            saveName = "AOIClientLog.txt",
            loggerEnum = LoggerType.Unity
        };
        PELog.InitSettings(config);

        client.StartAsClient("127.0.0.1", 18000);

        playerDic = new Dictionary<uint, GameObject>();
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
                    case CMD.NtfAOIMsg:
                        HandleNtfAOIMsg(pkg.ntfAOIMsg);
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
    /// �����¼��Ӧ
    /// </summary>
    void HandleLoginResponse(LoginResponse response)
    {
        entityIDTxt.text = $"EntityID: {response.entityID}";
        currentEntityID = response.entityID;
    }

    /// <summary>
    /// ����AOI��Ϣ
    /// </summary>
    void HandleNtfAOIMsg(NtfAOIMsg aoiMsg)
    {
        // �����˳���Ϣ
        if (aoiMsg.exitLst != null)
        {
            for (int i = 0; i < aoiMsg.exitLst.Count; i++)
            {
                ExitMsg exitMsg = aoiMsg.exitLst[i];
                if (playerDic.TryGetValue(exitMsg.entityID, out GameObject player))
                {
                    Destroy(player);
                }
            }
        }

        // ���������Ϣ
        if (aoiMsg.enterLst != null)
        {
            for (int i = 0; i < aoiMsg.enterLst.Count; i++)
            {
                EnterMsg enterMsg = aoiMsg.enterLst[i];
                if (!playerDic.ContainsKey(enterMsg.entityID))
                {
                    GameObject go = CommonTool.LoadItem(ItemEnum.EntityItem, enterMsg.entityID.ToString());
                    go.name = $"Entity: {enterMsg.entityID}";
                    go.transform.SetParent(entityRoot);

                    if (enterMsg.entityID == currentEntityID)
                    {
                        currentPlayerGo = go;
                        CommonTool.SetMaterialColor(currentPlayerGo, MaterialEnum.red);

                        if (CamFollow)
                        {
                            cam.transform.SetParent(currentPlayerGo.transform);
                            cam.transform.position = new Vector3(0, 40, 0);
                        }
                    }

                    go.transform.position = new Vector3(enterMsg.PosX, 0, enterMsg.PosZ);
                    if (!playerDic.ContainsKey(enterMsg.entityID))
                    {
                        playerDic.Add(enterMsg.entityID, go);
                    }
                }
            }
        }

        // �����ƶ���Ϣ
        if (aoiMsg.moveLst != null)
        {
            for (int i = 0; i < aoiMsg.moveLst.Count; i++)
            {
                MoveMsg mm = aoiMsg.moveLst[i];
                if (mm.entityID != currentEntityID)
                {
                    GameObject player;
                    if (playerDic.TryGetValue(mm.entityID, out player))
                    {
                        // ͬ��������ҵ�λ��
                        player.transform.position = new Vector3(mm.PosX, 0, mm.PosZ);
                    }
                }
            }
        }
    }

    public void AddMsgPackage(Package package)
    {
        packageQueue.Enqueue(package);
    }

    /// <summary>
    /// �����¼��ť�¼�
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
