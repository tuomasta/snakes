terraform {
  required_providers {
    helm = {
      source  = "hashicorp/helm"
      version = ">= 2.9.0"
    }
  }
}

variable "tag" {
  type = string
  default = "latest"
}

# install local helm chart to install the app
resource "helm_release" "snakesApp" {
  name       = "snakes-app"
  chart      = "./infra/chart"

  set {
    name  = "uriPath"
    value = ""
  }

  set {
    name  = "redis.loadbalancer"
    value = false
  }

  set {
    name  = "tag"
    value = var.tag
  }
}