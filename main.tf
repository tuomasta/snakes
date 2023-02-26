terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.44.1"
    }
  }
  backend "azurerm" {
    container_name = "terraform-state"
    key            = "snakes.tfstate"

    # use inline configurations with -backend-config=
    # resource_group_name  = "tuomas-general"
    # storage_account_name = "tuomascommonstorage"       
  }
}

variable "ssh_public_key_file" {
  type    = string
  default = "./snakes.pub"
}


provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = "snakes"
  location = "northeurope"
}

variable "tag" {
  type = string
  default = "latest"
}

module "cluster" {
  source              = "./infra/tf-modules/cluster/"
  ssh_public_key_file = var.ssh_public_key_file
  rg_name             = azurerm_resource_group.rg.name

  depends_on = [azurerm_resource_group.rg]
}

# authenticate to access the cluster
provider "helm" {
  kubernetes {
    host                   = module.cluster.host
    client_certificate     = base64decode(module.cluster.client_certificate)
    client_key             = base64decode(module.cluster.client_key)
    cluster_ca_certificate = base64decode(module.cluster.cluster_ca_certificate)
  }
}

module "ingress" {
  source     = "./infra/tf-modules/ingress/"
  depends_on = [module.cluster]
  providers = {
    helm = helm
  }
}


module "snakes-app" {
  source     = "./infra/tf-modules/snakes/"
  depends_on = [module.ingress]
  providers = {
    helm = helm
  }

  tag = var.tag
}

output "kube_config" {
  value     = module.cluster.kube_config
  sensitive = true
}
