﻿namespace GKE_Secret_Editor.Models;

public class GoogleCloudConnectionProperties
{
    public string Cluster { get; set; } = "";
    public string Zone { get; set; } = "";
    public string Project { get; set; } = "";

    public string Namespace { get; set; } = "";
}