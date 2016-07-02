import socket

def connection():

        # create a socket object
        serversocket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        # get local machine name
        host = socket.gethostname()
        BUFFER_SIZE = 1024  # Normally 1024, but we want fast response
        port = 9999
        host = [l for l in ([ip for ip in socket.gethostbyname_ex(socket.gethostname())[2] if not ip.startswith("127.")][:1], [
                [(s.connect(('8.8.8.8', 53)), s.getsockname()[0], s.close()) for s in
                 [socket.socket(socket.AF_INET, socket.SOCK_DGRAM)]][0][1]]) if l][0][0]
        serversocket.bind((host, port))
        print host
        # queue up to 5 requests
        serversocket.listen(1000)
        clientsocket, addr = serversocket.accept()
        print("Got a connection from %s" % str(addr))
        return clientsocket, BUFFER_SIZE

