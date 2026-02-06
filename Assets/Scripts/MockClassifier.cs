using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class MockClassifier : MonoBehaviour
{
    public static MockClassifier Instance;

    // -------------------------
    // CLASSIFIER DATA
    // -------------------------
    public List<ClassificationRecord> records = new List<ClassificationRecord>();
    private bool finalized = false;   // prevent multiple final prints

    void Awake()
    {
        Instance = this;
    }

    // ---------------------------------------------------------
    // Called from IntruderMeta.OnNeutralized / OnDestroyed
    // ---------------------------------------------------------
    public void RecordNeutralization(GameObject intruder, bool trueNeutralization)
    {
        if (finalized) return; // mission ended

        float confidence = Mathf.Lerp(0.5f, 1f, Random.value);
        Vector3 pos = intruder ? intruder.transform.position : Vector3.zero;

        records.Add(new ClassificationRecord
        {
            time = Time.time,
            x = pos.x,
            z = pos.z,
            confidence = confidence,
            trueNeutralized = trueNeutralization
        });

        if (records.Count % 10 == 0)
            ExportClassifierCSV();
    }

    // ---------------------------------------------------------
    // EXPORT CSV
    // ---------------------------------------------------------
    public void ExportClassifierCSV()
    {
        string path = Path.Combine(Application.persistentDataPath, "ClassifierRecords.csv");

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("time,x,z,confidence,trueNeutralized");

        foreach (var r in records)
            sb.AppendLine($"{r.time:F2},{r.x:F2},{r.z:F2},{r.confidence:F3},{(r.trueNeutralized ? 1 : 0)}");

        try
        {
            File.WriteAllText(path, sb.ToString());
            Debug.Log("[Classifier] Exported classifier CSV → " + path);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Classifier] CSV write error: " + ex.Message);
        }
    }

    // ---------------------------------------------------------
    // FINAL METRICS CALCULATED WHEN MISSION ENDS
    // ---------------------------------------------------------
    public void FinalizeMetrics()
    {
        if (finalized) return;
        finalized = true;

        int total = records.Count;
        int trueNeutral = 0;
        float avgConf = 0f;

        foreach (var r in records)
        {
            if (r.trueNeutralized) trueNeutral++;
            avgConf += r.confidence;
        }

        if (total > 0) avgConf /= total;

        float precision = (total > 0) ? (trueNeutral / (float)total) * 100f : 0f;

        Debug.Log("====== CLASSIFIER PRECISION REPORT ======");
        Debug.Log($"Total events: {total}");
        Debug.Log($"True neutralizations: {trueNeutral}");
        Debug.Log($"False neutralizations: {total - trueNeutral}");
        Debug.Log($"Precision: {precision:F2}%");
        Debug.Log($"Average confidence: {avgConf * 100f:F2}%");
        Debug.Log("=========================================");

        ExportClassifierCSV();
    }

    // ---------------------------------------------------------
    [System.Serializable]
    public struct ClassificationRecord
    {
        public float time;
        public float x;
        public float z;
        public float confidence;
        public bool trueNeutralized;
    }
}
