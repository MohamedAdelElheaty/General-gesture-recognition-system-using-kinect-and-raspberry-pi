# server.py
import connection
import actions

clientsocket, BUFFER_SIZE = connection.connection()

try:
    while True:
        # establish a connection
        data = clientsocket.recv(BUFFER_SIZE)
        #print (data)
        if (not data):
            break
        actions.options[data[0]](data)

finally:
        actions.HBridge.exit()
        clientsocket.close()
