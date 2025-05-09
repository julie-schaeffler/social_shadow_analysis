using System;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

public class DateSlider : MonoBehaviour
{
    public Slider dateSlider;
    public Text dateText;

    public InputField startDateInput;
    public InputField endDateInput;
    public InputField startTimeInput;
    public InputField endTimeInput;

    public DateTime startDate;
    public DateTime endDate;

    private int totalTime;

    void Start()
    {
        UpdateSliderRange();
    }

    void Update()
    {
        int minutesToAdd = Mathf.RoundToInt(dateSlider.value);
        DateTime currentDate = startDate.AddMinutes(minutesToAdd);
        SunCalculator sunCalculator = GameObject.Find("SunCalc").GetComponent<SunCalculator>();
        sunCalculator.m_Year = currentDate.Year;
        sunCalculator.m_Month = currentDate.Month;
        sunCalculator.m_Day = currentDate.Day;
        sunCalculator.m_Hour = currentDate.Hour;
        sunCalculator.m_Minute = currentDate.Minute;

        if (dateText != null)
        {
            dateText.text = currentDate.ToString("dd/MM/yyyy HH:mm:ss");
        }
    }

    public void UpdateSliderRange()
    {
        string dateFormat = "dd/MM/yyyy HH:mm:ss";
        CultureInfo provider = CultureInfo.InvariantCulture;

        if (string.IsNullOrEmpty(startDateInput.text) || string.IsNullOrEmpty(startTimeInput.text) ||
            string.IsNullOrEmpty(endDateInput.text) || string.IsNullOrEmpty(endTimeInput.text))
        {
            Debug.LogError("Please fill in all input fields.");
            return;
        }

        startDate = DateTime.ParseExact(startDateInput.text + " " + startTimeInput.text, dateFormat, provider);
        endDate = DateTime.ParseExact(endDateInput.text + " " + endTimeInput.text, dateFormat, provider);

        if (endDate < startDate)
        {
            Debug.LogError("The end date must be after the start date.");
            return;
        }

        totalTime = (int)(endDate - startDate).TotalMinutes;
        dateSlider.minValue = 0;
        dateSlider.maxValue = totalTime;
        dateSlider.value = 0;
    }
}