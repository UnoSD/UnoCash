#!/bin/bash

STORAGE_ACCOUNT=$(pulumi stack output StorageAccount)
RESOURCE_GROUP=$(pulumi stack output ResourceGroup)

az storage account update \
    --default-action Deny \
    --bypass None \
    --name $STORAGE_ACCOUNT