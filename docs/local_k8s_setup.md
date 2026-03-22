Install Podman Desktop, choose Kubernetes as well when prompted

https://podman-desktop.io/tutorial/running-a-kubernetes-cluster

If the minikube tab is not visible after activating the extension, run this in the terminal:
choco install minikube oder winget install Kubernetes.minikube 
and then restart podman

After finishing that tutorial, you should have a config file in the C:\Users\{user}\.kube folder. This is the localhost config
Then you can go to the link https://pm4.init-lab.ch/dashboard/c/local/explorer#cluster-events and click on the file icon in the top right corner "download KubeConfig"
Copy this file into your .kube folder as well and rename it to config-pm4 (without file extension)
In powershell, run these 2 commands to merge the files together:
$env:KUBECONFIG="$HOME\.kube\config;$HOME\.kube\config-pm4"
kubectl config view --merge --flatten > $HOME\.kube\config-merged
This should generate a config-merged file in the folder. Rename the config to config-local and the config-merged to config
Done. If you now run kubectl config get-contexts in the terminal, you should see pm4 and minikube

Useful Commands:
kubectl config get-contexts -> Shows different "servers" (Imagine a Kubernetes context is like a host-server, namespace is like an environment)
kubectl config use-context minikube -> switch "host-server" to minikube (localhost)
kubectl config use-context pm4 -> switch to pm4 (remote with staging/prod namespaces)

**IMPORTANT**
kubectl config set-context --current --namespace=mr-staging -> Set current namespace to staging
kubectl config set-context --current --namespace=mr-prod -> Set current namespace to prod

kubectl config view --minify --output 'jsonpath={.current-context}{" / "}{..namespace}{"\n"}' -> check in which context and namespace you are currently connected

kubectl get pods -> check pods on current context and namespace


**APPLYING CONFIGS EXAMPLES**

FOR LOCAL: kubectl apply -f k8s/base 
and then choose the image version you want from https://github.com/MunozRaul/MysticRiver/pkgs/container/mysticriver%2Fmysticriver-api (commit hash)
kubectl set image deployment/mysticriver-api mysticriver-api=ghcr.io/munozraul/mysticriver/mysticriver-api:{COMMIT_HASH_HERE}

FOR REMOTE: kubectl --context=pm4 -n mr-staging apply -f k8s/base

To test the API locally, you can port forward from the local k8s cluster in the terminal:
kubectl port-forward svc/mysticriver-api 8080:80
and then you can access the API at localhost:8080/WeatherForecast