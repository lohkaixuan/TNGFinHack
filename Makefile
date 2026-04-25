AWS_ACCOUNT_ID ?= 375590654616
AWS_REGION ?= ap-southeast-1
AWS_PROFILE ?= 375590654616_finhack_IsbUsersPS
ECR_REPOSITORY ?= apiapp
IMAGE_TAG ?= latest
DOCKER_PLATFORM ?= linux/amd64
ECR_IMAGE = $(AWS_ACCOUNT_ID).dkr.ecr.$(AWS_REGION).amazonaws.com/$(ECR_REPOSITORY):$(IMAGE_TAG)
AWS = aws --profile $(AWS_PROFILE) --region $(AWS_REGION)

.PHONY: aws-sso aws-check aws-login aws-build aws-tag aws-push aws-image aws-print

aws-sso:
	aws configure sso --profile $(AWS_PROFILE)
	aws sso login --profile $(AWS_PROFILE)

aws-check:
	@command -v aws >/dev/null 2>&1 || { echo "AWS CLI is not installed. Install it first: brew install awscli"; exit 1; }
	@command -v docker >/dev/null 2>&1 || { echo "Docker is not installed or not running."; exit 1; }
	@$(AWS) sts get-caller-identity >/dev/null 2>&1 || { echo "AWS profile is not logged in. Run: make aws-sso"; exit 1; }

aws-login: aws-check
	$(AWS) ecr get-login-password | docker login --username AWS --password-stdin $(AWS_ACCOUNT_ID).dkr.ecr.$(AWS_REGION).amazonaws.com

aws-build:
	docker build --platform $(DOCKER_PLATFORM) -t $(ECR_REPOSITORY):$(IMAGE_TAG) Server/ApiApp

aws-tag:
	docker tag $(ECR_REPOSITORY):$(IMAGE_TAG) $(ECR_IMAGE)

aws-push:
	docker push $(ECR_IMAGE)

aws-image: aws-login aws-build aws-tag aws-push
	@echo "Pushed $(ECR_IMAGE)"

aws-print:
	@echo "$(ECR_IMAGE)"
