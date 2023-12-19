# InternalTest

For this test you will need to get the following apis and application running and connected via dapr.

Please put all dapr yaml files in Config/Components file.

Documentation for dapr https://docs.dapr.io/

If you choose to run this in self-hosted mode, you will also need to run a rabbitmq instance in your docker.

If you choose to run on a kubernetes array, we advise you to use minikube for local use.

The overall objective is to get the ConsoleApp to invoke all the endpoints of the AlertApi, one at a time.

You will be able to test all the endpoints on insomnia or postman (once code has been filled in).
