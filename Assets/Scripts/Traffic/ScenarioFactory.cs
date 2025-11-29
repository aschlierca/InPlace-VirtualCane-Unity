using UnityEngine;

public class ScenarioFactory : MonoBehaviour
{
    [SerializeField] private GameObject scenarioPrefab;

    public ScenarioManager GetScenario()
    {
        GameObject instance = Instantiate(scenarioPrefab.gameObject);
        ScenarioManager newScenario = instance.GetComponent<ScenarioManager>();

        return newScenario;
    }
}