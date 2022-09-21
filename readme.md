# Snakes
This is my simple playground project.

It implements distributed snakes game. Snakes was chosen because implementing the game rules are simple, allowing me to focus on the tech.

The aim is to test out following technologies:
- F#
- SignalR
- Kubernetis, hosting (todo)
- Redis, signalr backplane
- Blazor, client (todo)
- AKS & Github deployments (todo)

## BUILD & RUN WITH DOCKER

### BUILD
``docker build -f [client|server]/Dockerfile -t snakes-[client|server] .``

### NETWORK
``docker network create snakes``

### RUN
##### SERVER
``docker run --name snakes-server -p 5000:80 --network snakes snakes-server``

##### CLIENT
``docker run -it --network snakes -e "SERVER_URI=http://snakes-server:80" snakes-client``