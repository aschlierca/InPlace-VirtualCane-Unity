using UnityEngine;

public class CarFactory : MonoBehaviour
{
    [SerializeField] private GameObject carPrefab;

    public Car GetCar()
    {
        GameObject instance = Instantiate(carPrefab.gameObject);
        Car newCar = instance.GetComponent<Car>();
        //instance.SetActive(true);

        return newCar;
    }
}