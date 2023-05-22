Tests project

### Setup Standalone Database Docker

Install Milvus Standalone with Docker Compose https://milvus.io/docs/install_standalone-docker.md

### Run tests with Docker-Compose

- The below command will start all dependencies such as database (standalone, etcd, minio) and then run the tests
  project
- It means that you don't need to manual setup your own database for running the tests prpject

```shell
docker-compose up --build tests-project
```

Use this to clean up everything

```shell
docker-compose down
```

### Getting Started

- In each test case, a new collection will be created prior and dropped after the test is completed
- Test data is designed based on the milvus's User Guide: https://milvus.io/docs/create_collection.md

