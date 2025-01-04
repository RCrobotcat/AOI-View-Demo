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

    float horizontal, vertical;

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

    void FixedUpdate()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, vertical);

        if (direction != Vector3.zero)
        {
            if (currentEntityID != 0 && currentPlayerGo != null)
            {
                currentPlayerGo.transform.position += direction.normalized * RegularConfigs.moveSpeed * Time.fixedDeltaTime;
                Package pkg = new Package
                {
                    cmd = CMD.SendMovePos,
                    sendMovePos = new SendMovePos
                    {
                        entityID = currentEntityID,
                        PosX = currentPlayerGo.transform.position.x,
                        PosZ = currentPlayerGo.transform.position.z
                    }
                };
                client.session.SendMsg(pkg);
            }
        }
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
                    case CMD.NtfCell:
                        HandleCreateCell(pkg.ntfCell);
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
                    if (playerDic.Remove(exitMsg.entityID))
                        Destroy(player);
                    else
                        this.Error($"Failed to remove player from playerDic with entityID: {exitMsg.entityID}.");
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
    /// <summary>
    /// ������Cell
    /// </summary>
    void HandleCreateCell(NtfCell ntfCell)
    {
        GameObject go = CommonTool.LoadItem(ItemEnum.CellItem, $"{ntfCell.xIndex.ToString()}_{ntfCell.zIndex.ToString()}");
        go.transform.SetParent(cellRoot);
        go.transform.localPosition = new Vector3(ntfCell.xIndex * RegularConfigs.aoiSize + RegularConfigs.aoiSize / 2, 0,
            ntfCell.zIndex * RegularConfigs.aoiSize + RegularConfigs.aoiSize / 2);
        go.transform.localScale = new Vector3(RegularConfigs.aoiSize * 0.99f, 1, RegularConfigs.aoiSize * 0.99f);
        CommonTool.SetCellColor(go);
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

    private void OnApplicationQuit()
    {
        client.session.SendMsg(new Package
        {
            cmd = CMD.SendExitStage,
            sendExit = new SendExit
            {
                entityID = currentEntityID
            }
        });

        client.CloseClient();
    }
}
