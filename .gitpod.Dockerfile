FROM gitpod/workspace-full-vnc

USER gitpod

RUN sudo apt-get update && sudo apt-get install -y wget git
RUN sudo wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN sudo dpkg -i packages-microsoft-prod.deb
RUN sudo rm packages-microsoft-prod.deb
RUN sudo apt-get install -y dotnet-sdk-6.0
RUN pip install opencv-python