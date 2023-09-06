using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Mapbox.CheapRulerCs;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class StoneListEntryBehaviour : MonoBehaviour, IPointerClickHandler
{
    public float updateInterval = 3;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI addressText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI unitText;
    
    private GameObject player;
    private GameObject stone;
    private StoneListBehaviour list;
    private AbstractMap map;
    private Navigation navigation;
    private CheapRuler ruler;
    
    // Start is called before the first frame update
    void Start()
    {
        map = FindObjectOfType<AbstractMap>();
        navigation = FindObjectOfType<Navigation>();
        list = FindObjectOfType<StoneListBehaviour>();
        list.entries.Add(this);
        player = list.playerObject;
        
        StartCoroutine(UpdateDistance());
    }

    public void Init(GameObject stoneGameObject, string name, string address)
    {
        stone = stoneGameObject;
        nameText.text = name;
        addressText.text = address;
    }
    
    private IEnumerator UpdateDistance()
    {
        if (player == null || stone == null)
            yield break;
        
        while (true)
        {
            yield return new WaitUntil(() => gameObject.activeSelf);
            
            var playerGeoPos = map.WorldToGeoPosition(player.transform.position);
            var stoneGeoPos = map.WorldToGeoPosition(stone.transform.position);

            if (ruler == null)
            {
                ruler = new CheapRuler(playerGeoPos.x, CheapRulerUnits.Meters);
            }

            var pointA = new double[] {playerGeoPos.x, playerGeoPos.y};
            var pointB = new double[] {stoneGeoPos.x, stoneGeoPos.y};
            var distance = ruler.Distance(pointA, pointB);
            
            if (distance < 1000)
            {
                var distanceMeters = Math.Round(distance, 2, MidpointRounding.ToEven);
                distanceText.text = distanceMeters.ToString(CultureInfo.InvariantCulture);
                unitText.text = "Meters";
            }
            else
            {
                var distanceKilometers = Math.Round(distance / 1000, 2, MidpointRounding.ToEven);
                distanceText.text = distanceKilometers.ToString(CultureInfo.InvariantCulture);
                unitText.text = "Kilometers";
            }
            
            yield return new WaitForSeconds(updateInterval);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (navigation.isNavigationMode)
        {
            navigation.EndNavigationMode();
        }
        
        navigation.CalculateDirectionsToSelectedStone(stone);
        list.Close();
    }
}
