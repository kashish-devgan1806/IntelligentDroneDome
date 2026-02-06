using UnityEngine;

/// <summary>
/// Extended Kalman Filter for tracking intruder position and velocity.
/// Provides smoothed estimates and covariance for predictive interception.
/// </summary>
public class KalmanTracker
{
    // State: [x, y, z, vx, vy, vz]
    private Vector3 position;
    private Vector3 velocity;

    // Covariance matrix (simplified 6x6 → stored as two 3x3 blocks)
    private Matrix3x3 posCovariance;
    private Matrix3x3 velCovariance;

    // Process noise (how much we trust the motion model)
    private float processNoisePosQ = 0.5f;
    private float processNoiseVelQ = 1.0f;

    // Measurement noise (how much we trust the sensor)
    private float measurementNoiseR = 2.0f;

    public KalmanTracker(Vector3 initialPos, Vector3 initialVel)
    {
        position = initialPos;
        velocity = initialVel;

        // Initialize covariance with moderate uncertainty
        posCovariance = Matrix3x3.Identity() * 10f;
        velCovariance = Matrix3x3.Identity() * 5f;
    }

    /// <summary>
    /// Prediction step: propagate state forward by dt using constant velocity model.
    /// </summary>
    public void Predict(float dt)
    {
        // State prediction: x = x + v*dt
        position += velocity * dt;

        // Covariance prediction: P = F*P*F^T + Q
        // For constant velocity: position uncertainty grows with velocity uncertainty
        posCovariance = posCovariance.Add(velCovariance.Scale(dt * dt)).Add(Matrix3x3.Identity().Scale(processNoisePosQ * dt));
        velCovariance = velCovariance.Add(Matrix3x3.Identity().Scale(processNoiseVelQ * dt));
    }

    /// <summary>
    /// Update step: correct prediction with sensor measurement.
    /// </summary>
    public void Update(Vector3 measuredPos, float measurementNoise = -1f)
    {
        if (measurementNoise < 0f) measurementNoise = measurementNoiseR;

        // Kalman gain for position: K = P / (P + R)
        Matrix3x3 S = posCovariance.Add(Matrix3x3.Identity().Scale(measurementNoise));
        Matrix3x3 K = posCovariance.Multiply(S.Inverse());

        // Innovation: difference between measurement and prediction
        Vector3 innovation = measuredPos - position;

        // Correct state
        position += K.MultiplyVector(innovation);

        // Update covariance: P = (I - K) * P
        posCovariance = Matrix3x3.Identity().Subtract(K).Multiply(posCovariance);

        // Velocity update from position change (simple derivative estimate)
        // More sophisticated: use position history, but for real-time keep it simple
    }

    /// <summary>
    /// Update velocity estimate directly (if available from fused track).
    /// </summary>
    public void UpdateVelocity(Vector3 measuredVel)
    {
        // Simple weighted average (Kalman-like)
        float alpha = 0.3f; // trust new measurement moderately
        velocity = Vector3.Lerp(velocity, measuredVel, alpha);
    }

    /// <summary>
    /// Predict future position at time t seconds ahead.
    /// </summary>
    public Vector3 PredictPosition(float t)
    {
        return position + velocity * t;
    }

    /// <summary>
    /// Get current filtered position.
    /// </summary>
    public Vector3 GetPosition() => position;

    /// <summary>
    /// Get current filtered velocity.
    /// </summary>
    public Vector3 GetVelocity() => velocity;

    /// <summary>
    /// Get position uncertainty (trace of covariance).
    /// </summary>
    public float GetPositionUncertainty()
    {
        return posCovariance.Trace();
    }
}

/// <summary>
/// Simple 3x3 matrix for covariance calculations.
/// </summary>
public struct Matrix3x3
{
    public float m00, m01, m02;
    public float m10, m11, m12;
    public float m20, m21, m22;

    public static Matrix3x3 Identity()
    {
        return new Matrix3x3
        {
            m00 = 1, m01 = 0, m02 = 0,
            m10 = 0, m11 = 1, m12 = 0,
            m20 = 0, m21 = 0, m22 = 1
        };
    }

    // Operator overload for scalar multiplication
    public static Matrix3x3 operator *(Matrix3x3 mat, float scalar)
    {
        return mat.Scale(scalar);
    }

    public static Matrix3x3 operator *(float scalar, Matrix3x3 mat)
    {
        return mat.Scale(scalar);
    }

    public Matrix3x3 Scale(float s)
    {
        return new Matrix3x3
        {
            m00 = m00 * s, m01 = m01 * s, m02 = m02 * s,
            m10 = m10 * s, m11 = m11 * s, m12 = m12 * s,
            m20 = m20 * s, m21 = m21 * s, m22 = m22 * s
        };
    }

    public Matrix3x3 Add(Matrix3x3 other)
    {
        return new Matrix3x3
        {
            m00 = m00 + other.m00, m01 = m01 + other.m01, m02 = m02 + other.m02,
            m10 = m10 + other.m10, m11 = m11 + other.m11, m12 = m12 + other.m12,
            m20 = m20 + other.m20, m21 = m21 + other.m21, m22 = m22 + other.m22
        };
    }

    public Matrix3x3 Subtract(Matrix3x3 other)
    {
        return new Matrix3x3
        {
            m00 = m00 - other.m00, m01 = m01 - other.m01, m02 = m02 - other.m02,
            m10 = m10 - other.m10, m11 = m11 - other.m11, m12 = m12 - other.m12,
            m20 = m20 - other.m20, m21 = m21 - other.m21, m22 = m22 - other.m22
        };
    }

    public Matrix3x3 Multiply(Matrix3x3 other)
    {
        return new Matrix3x3
        {
            m00 = m00 * other.m00 + m01 * other.m10 + m02 * other.m20,
            m01 = m00 * other.m01 + m01 * other.m11 + m02 * other.m21,
            m02 = m00 * other.m02 + m01 * other.m12 + m02 * other.m22,

            m10 = m10 * other.m00 + m11 * other.m10 + m12 * other.m20,
            m11 = m10 * other.m01 + m11 * other.m11 + m12 * other.m21,
            m12 = m10 * other.m02 + m11 * other.m12 + m12 * other.m22,

            m20 = m20 * other.m00 + m21 * other.m10 + m22 * other.m20,
            m21 = m20 * other.m01 + m21 * other.m11 + m22 * other.m21,
            m22 = m20 * other.m02 + m21 * other.m12 + m22 * other.m22
        };
    }

    public Vector3 MultiplyVector(Vector3 v)
    {
        return new Vector3(
            m00 * v.x + m01 * v.y + m02 * v.z,
            m10 * v.x + m11 * v.y + m12 * v.z,
            m20 * v.x + m21 * v.y + m22 * v.z
        );
    }

    public float Trace()
    {
        return m00 + m11 + m22;
    }

    public Matrix3x3 Inverse()
    {
        // Simplified 3x3 inverse (assumes non-singular)
        float det = m00 * (m11 * m22 - m12 * m21)
                  - m01 * (m10 * m22 - m12 * m20)
                  + m02 * (m10 * m21 - m11 * m20);

        if (Mathf.Abs(det) < 1e-6f) return Identity(); // fallback

        float invDet = 1f / det;

        return new Matrix3x3
        {
            m00 = (m11 * m22 - m12 * m21) * invDet,
            m01 = (m02 * m21 - m01 * m22) * invDet,
            m02 = (m01 * m12 - m02 * m11) * invDet,

            m10 = (m12 * m20 - m10 * m22) * invDet,
            m11 = (m00 * m22 - m02 * m20) * invDet,
            m12 = (m02 * m10 - m00 * m12) * invDet,

            m20 = (m10 * m21 - m11 * m20) * invDet,
            m21 = (m01 * m20 - m00 * m21) * invDet,
            m22 = (m00 * m11 - m01 * m10) * invDet
        };
    }
}