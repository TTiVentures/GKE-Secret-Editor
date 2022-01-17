# GKE-Secret-Editor
Editor for Google Kubernetes Engine Secrets

This only works for Windows for now

## Usage
1. Ensure you have installed Google Cloud SDK Shell

2. Execute first time to create "properties.json" 
inside the folder "_data" (all this will be auto generated)

3. Edit the parameters of this folder to connect to the secrets 
of your cluster:
```
{
  "Cluster": "your-cluster",
  "Zone": "your-zone",
  "Project": "your-project",
  "Namespace": "your-namespace"
}
```
