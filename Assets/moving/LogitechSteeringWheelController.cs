using UnityEngine;

public class ElectricCarController : MonoBehaviour
{
    public float maxTorque = 150f; // 최대 토크
    public float currentSpeed = 0f; // 현재 속도 (km/h)
    public float baseTorque = 50f; // 기본 토크
    public float baseBrakeTorque = 0f; // 기본 브레이크 토크

    public float maxSteeringAngle = 70f; // 최대 조향 각도
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    private float throttleInput = 0f; // 가속 입력
    private float brakeInput = 0f; // 브레이크 입력
    private float steeringAngle = 0f; // 조향 각도

    public Rigidbody carRigidbody; // 차량의 Rigidbody

    private float motorTorque = 0f;
    private float antiRollForce = 500f; // Anti-roll force 값 정의

    public enum Gear { P, R, N, D }
    public Gear currentGear = Gear.P;

    void Start()
    {
        LogitechGSDK.LogiSteeringInitialize(false);
        carRigidbody = GetComponent<Rigidbody>();
        carRigidbody.mass = 1800f; // 아이오닉 5의 대략적인 무게
    }

    void Update()
    {
        if (!LogitechGSDK.LogiUpdate())
        {
            Debug.LogError("Logitech device not found!");
            return;
        }

        HandleInput();
        ApplySteering();
        ApplyThrottle();
        UpdateSpeed(); // 속도 계산

        ApplyAntiRoll(frontLeftWheel, frontRightWheel); // Anti-roll 효과 적용
        AirResistance(); // 공기저항

        ApplyForceFeedback(); // Force Feedback 추가
        LogStatus();
    }

    void HandleInput()
    {
        var wheelState = LogitechGSDK.LogiGetStateUnity(0);

        if (wheelState.rgbButtons[15] == 128) currentGear = Gear.D;
        else if (wheelState.rgbButtons[14] == 128) currentGear = Gear.R;
        else if (wheelState.rgbButtons[12] == 128) currentGear = Gear.P;
        else currentGear = Gear.N;

        throttleInput = (32767 - wheelState.lY) / 32767f / 2; // 악셀 입력
        brakeInput = (32767 - wheelState.lRz) / 32767f / 2; // 브레이크 입력
        steeringAngle = (wheelState.lX / 32767f) * maxSteeringAngle; // 조향 입력
    }

    void UpdateSpeed()
    {
        // WheelCollider의 회전 속도 기반으로 차량의 속도 계산
        float wheelRadius = frontLeftWheel.radius;  // 앞바퀴 반지름
        float wheelRPM = frontLeftWheel.rpm; // 바퀴의 회전수 (RPM)

        // 차량의 속도 계산 (속도 = 회전수 * 바퀴의 원주 / 시간)
        // 1RPM = 2 * PI * 반지름 / 분 -> km/h로 변환
        float calculatedSpeed = (wheelRPM * 2 * Mathf.PI * wheelRadius * 60f) / 1000f;

        // WheelCollider의 GetGroundHit()을 사용하여 휠이 실제로 땅과 접지 상태인지 확인
        WheelHit hit;
        if (frontLeftWheel.GetGroundHit(out hit))
        {
            // 휠의 속도 벡터를 이용해 현재 속도 계산
            Vector3 wheelVelocity = frontLeftWheel.attachedRigidbody.GetPointVelocity(frontLeftWheel.transform.position);
            currentSpeed = wheelVelocity.magnitude * 3.6f; // m/s -> km/h 변환
        }
        else
        {
            // 바퀴가 지면과 접지하지 않으면 속도는 0
            currentSpeed = 0f;
        }

        // 정지 상태에서 속도가 0으로 설정되도록 처리
        if (currentSpeed < 1f)
        {
            currentSpeed = 0f;
        }
    }

    void ApplySteering()
    {
        frontLeftWheel.steerAngle = steeringAngle;
        frontRightWheel.steerAngle = steeringAngle;
    }

    void ApplyThrottle()
    {
        if (currentSpeed > 15) {
            baseTorque = 0f;
        }
        else
        {
            baseTorque = 50f;
        }

        if (currentGear == Gear.D)
        {
            motorTorque = baseTorque + throttleInput * 200 * 10f; // 가속 입력에 따라 토크 계산
            
        }
        else if (currentGear == Gear.R)
        {
            motorTorque = -(baseTorque + throttleInput * 200 * 10f); // 가속 입력에 따라 토크 계산
        }
        else if (currentGear == Gear.N)
        {
            motorTorque = 0.0001f;
        }
        else {
            motorTorque = 0f;
        }

        frontLeftWheel.motorTorque = motorTorque;
        frontRightWheel.motorTorque = motorTorque;

        // 브레이크가 아닌 경우 브레이크 토크를 0으로 설정
        if (brakeInput > 0)
        {
            ApplyBrakes(brakeInput * 700f);
        }
        else if (currentGear == Gear.P)
        {
            ApplyBrakes(5000f);
        }
        else
        {
            frontLeftWheel.brakeTorque = 0f;
            frontRightWheel.brakeTorque = 0f;
            rearLeftWheel.brakeTorque = 0f;
            rearRightWheel.brakeTorque = 0f;
        }
    }

    void ApplyBrakes(float brakeTorque)
    {
        // float brakeTorque = baseBrakeTorque + brakeInput * 700f;

        frontLeftWheel.brakeTorque = brakeTorque;
        frontRightWheel.brakeTorque = brakeTorque;
        rearLeftWheel.brakeTorque = brakeTorque;
        rearRightWheel.brakeTorque = brakeTorque;
    }

    void ApplyForceFeedback()
    {
        float speedThreshold = 0.1f; // 중립 복귀력이 적용되는 최소 속도

        if (currentSpeed > speedThreshold)
        {
            float springForceCentering = Mathf.Clamp((currentSpeed + 1) * 0.5f, 0, 50);  // 속도에 따른 스티어링 복귀력
            LogitechGSDK.LogiPlaySpringForce(0, 0, 20, (int)springForceCentering);
        }
        else
        {
            LogitechGSDK.LogiStopSpringForce(0);
        }
    }

    void ApplyAntiRoll(WheelCollider wheelL, WheelCollider wheelR)
    {
        WheelHit hit;
        float travelL = 1.0f;
        float travelR = 1.0f;

        bool groundedL = wheelL.GetGroundHit(out hit);
        if (groundedL)
        {
            travelL = (-wheelL.transform.InverseTransformPoint(hit.point).y - wheelL.radius) / wheelL.suspensionDistance;
        }

        bool groundedR = wheelR.GetGroundHit(out hit);
        if (groundedR)
        {
            travelR = (-wheelR.transform.InverseTransformPoint(hit.point).y - wheelR.radius) / wheelR.suspensionDistance;
        }

        float antiRollForceCalculation = (travelL - travelR) * antiRollForce; // antiRollForce 계산

        if (groundedL)
        {
            GetComponent<Rigidbody>().AddForceAtPosition(wheelL.transform.up * antiRollForceCalculation, wheelL.transform.position);
        }
        if (groundedR)
        {
            GetComponent<Rigidbody>().AddForceAtPosition(wheelR.transform.up * antiRollForceCalculation, wheelR.transform.position);
        }
    }

    private void AirResistance()
    {
        float airDensity = 1.2f;
        float dragCoefficient = 0.3f;
        float forntalArea = 2.2f;

        // 공기 저항 계산 (N)
        float airResistance = 0.5f * airDensity * dragCoefficient * forntalArea * Mathf.Pow(currentSpeed, 2);

        carRigidbody.AddForce(-carRigidbody.velocity.normalized * airResistance);
    }

    void LogStatus()
    {
        Debug.Log($"ThrottleInput: {throttleInput:F2}, BrakeInput: {brakeInput:F2}, Speed: {(int)currentSpeed} km/h, currentGear: {currentGear}, MortorTorque: {motorTorque}");
    }

    void OnDestroy()
    {
        LogitechGSDK.LogiSteeringShutdown(); // 프로그램 종료 시 SDK 해제
    }
}