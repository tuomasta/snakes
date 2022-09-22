# Snakes
This is my simple playground project.

It implements distributed snakes game. Snakes was chosen because implementing the game rules are simple, allowing me to focus on the tech.

The aim is to test out following technologies:
- F#
- SignalR
- Kubernetis, hosting
- Redis, signalr backplane
- Blazor, client (todo)
- AKS & Github deployments (todo)

## BUILD & RUN WITH DOCKER

### BUILD
``docker build -f [client|server|executor]/Dockerfile -t talustuo/snakes-[client|server|executor] .``

### NETWORK
``docker network create snakes``

### RUN
##### Redis
``docker run --rm --name redis -p 6379:6379 --network snakes redislabs/redismod``

##### Executor
``docker run --rm --name snakes-executor -e "REDIS_URI=redis:6379" --network snakes talustuo/snakes-executor``

##### SERVER
``docker run --rm --name snakes-server -e "REDIS_URI=redis:6379" -p 5000:80 --network snakes talustuo/snakes-server``

##### CLIENT
``docker run --rm --name snakes-client -it --network snakes -e "SERVER_URI=http://snakes-server:80" talustuo/snakes-client``

## K8S

#### DEPLOY
``kubectl apply -f .\k8s\redis.yaml``

``kubectl apply -f .\k8s\snakes-deployment.yaml``

#### ATTACH TO CLIENT RUNNING IN K8S
``kubectl get pods``

``kubectl attach -it client-dpt-<xxx>``