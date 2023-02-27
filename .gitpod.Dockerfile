FROM gitpod/workspace-full-vnc

USER gitpod

RUN sudo apt-get update
RUN sudo apt-get install -y dotnet-sdk-6.0
RUN pip install opencv-python