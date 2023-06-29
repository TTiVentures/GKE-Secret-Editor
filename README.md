# GKE-Secret-Editor

Editor for Google Kubernetes Engine Secrets

This only works for Windows for now

## Usage

1. Ensure you have installed Google Cloud SDK Shell

- WINDOWS: https://cloud.google.com/sdk/docs/quickstart-windows

2. Install the plugin for kubectl

```
gcloud components install gke-gcloud-auth-plugin
```

3. Execute first time to create "properties.json"
   inside the folder "\_data" (the folder and the file will be created automatically)

4. Edit the parameters of this folder to connect to the secrets
   of your cluster:

```json
{
  "Cluster": "your-cluster",
  "Zone": "your-zone",
  "Project": "your-project",
  "Namespace": "your-namespace"
}
```
