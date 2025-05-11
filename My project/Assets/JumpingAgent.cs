using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Linq;

public class JumpAgent : Agent
{
    public Rigidbody rigidB;
    public float jumpForce = 5f;
    public float moveSpeed = 5f;
    public float gravityMod = 3;
    public bool isGrounded = true;

    public GameObject obstacle;
    private Vector3 obstacleMoveDirection;
    private float obstacleSpeed;

    private bool badEnd;
    private bool goodEnd;

    public Vector3 startPosition;
    private bool despawnObjects = false;

    private bool jump = true;

    public int RandomItemType;

    public override void OnEpisodeBegin()
    {
        if (rigidB == null)
            rigidB = GetComponent<Rigidbody>();

        rigidB.velocity = Vector3.zero;

        if (this.transform.localPosition.y < 0 || badEnd)
        {
            badEnd = false;
            this.transform.localPosition = startPosition;
            this.transform.localRotation = Quaternion.identity;
        }

        int obstacleDirection = Random.Range(0, 2);

        if (obstacleDirection == 0)
        {
            obstacle.transform.localPosition = new Vector3(-20f, 0.5f, 0);
            obstacle.transform.eulerAngles = new Vector3(0, 0, 0);
            obstacleMoveDirection = new Vector3(Random.Range(10, 20) * Time.deltaTime, 0, 0);
        }
        else
        {
            obstacle.transform.localPosition = new Vector3(0, 0.5f, -20f);
            obstacle.transform.eulerAngles = new Vector3(0, -90, 0);
            obstacleMoveDirection = new Vector3(Random.Range(10, 20) * Time.deltaTime, 0, 0);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(isGrounded);
        sensor.AddObservation(this.transform.localPosition);
        sensor.AddObservation(RandomItemType);
    }

    public void giveRewardExternally(float rewardAmount)
    {
        Debug.Log($"reward of {rewardAmount} given");
        SetReward(rewardAmount);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
       
        obstacle.transform.Translate(obstacleMoveDirection);

        float horizontalInput = actionBuffers.ContinuousActions.Length > 0 ? actionBuffers.ContinuousActions[0] : 0;
        Vector3 controlSignal = new Vector3(horizontalInput * moveSpeed, 0, 0);
        transform.Translate(controlSignal * Time.deltaTime);

        int jumpAction = actionBuffers.DiscreteActions[0];
        if (jumpAction == 1 && jump)
        {
            jump = false;
            isGrounded = false;
            rigidB.AddForce(Vector3.up * jumpForce * 10, ForceMode.Impulse);
            SetReward(-0.01f);
        }

        if (badEnd)
        {
            SetReward(-1f);
            EndEpisode();
        }
        else if (goodEnd)
        {
            goodEnd = false;
            SetReward(1.5f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");

        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = Input.GetAxis("Vertical") > 0 ? 1 : 0;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 7)
            badEnd = true;
        if (collision.gameObject.layer == 6)
        {
            isGrounded = true;
            jump = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.tag);
        if (other.gameObject.CompareTag("reward"))
        {
            SetReward(0.7f);
            Debug.Log("Hit reward sphere!!!");
            other.gameObject.SetActive(false);
        }
        else if (other.gameObject.CompareTag("obstacle"))
        {
            SetReward(-1.0f);
            Debug.Log("Obstacle hit!!!");
            foreach (var item in GameObject.FindGameObjectsWithTag("obstacle").Concat(GameObject.FindGameObjectsWithTag("reward")))
            {
                Destroy(item);
            }
            EndEpisode();
        }
        else if (other.gameObject.CompareTag("floor"))
        {
            jump = true;
            isGrounded = true;
        }
    }

    public void EndReached()
    {
        goodEnd = true;
    }
}
