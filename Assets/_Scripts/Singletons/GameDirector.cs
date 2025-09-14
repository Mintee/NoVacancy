using UnityEngine;
using Unity.Cinemachine;

public class GameDirector : MonoBehaviour
{
    [Header("Scene Setup")]
    [SerializeField] private Transform playerSpawn;           // required
    [SerializeField] private GameObject playerPrefab;         // optional if player already exists
    [SerializeField] private CinemachineCamera cmShoulder;    // your scene vCam

    private GameObject _player;

}