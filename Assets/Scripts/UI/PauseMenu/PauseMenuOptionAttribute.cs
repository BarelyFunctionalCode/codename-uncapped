using System;
using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
public class PauseMenuOptionAttribute : PropertyAttribute
{
  public string label;
  public string tabName;
  public string categoryName;
  public float minValue;
  public float maxValue;

  public PauseMenuOptionAttribute()
  {
  }

  public PauseMenuOptionAttribute(string label, string tabName = "", string categoryName = "", float minValue = -1.0f, float maxValue = -1.0f)
  {
    this.label = label;
    this.tabName = tabName;
    this.categoryName = categoryName;
    this.minValue = minValue;
    this.maxValue = maxValue;
  }
}

[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
public class PauseMenuDevOptionAttribute : PauseMenuOptionAttribute
{
  public PauseMenuDevOptionAttribute()
  {
  }

  public PauseMenuDevOptionAttribute(string label, float minValue = -1.0f, float maxValue = -1.0f)
  {
    this.label = label;
    this.minValue = minValue;
    this.maxValue = maxValue;
  }
}