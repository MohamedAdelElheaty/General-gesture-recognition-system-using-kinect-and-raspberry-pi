# define the function blocks
import L298NHBridge as HBridge


lastAction= 99

def noAction(Data):
    speedleft = 0
    speedright = 0
    HBridge.setMotorLeft(speedleft)
    HBridge.setMotorRight(speedright)
    return

def turnRight(speedleft,speedright,Data):

        if Data[0]=='1':
            speedright = -0.3
            print ("Reverse")
        else :
            speedright = 0.3

        print (Data[0])
        HBridge.setMotorLeft(speedleft)
        HBridge.setMotorRight(speedright)
        lastAction=Data
        #turn off GPIO
        return

def turnLeft(speedleft,speedright,Data):
        if Data[0]=='1':
            speedleft = -0.3
            print ("Reverse")
        else :
            speedleft = 0.3
        HBridge.setMotorLeft(speedleft)
        HBridge.setMotorRight(speedright)
        lastAction=Data
        return

def center(speedleft,speedright,Data):
        lastAction=Data;
        return

def highSpeed(Data):
        #turn on GPIO
        #delay
        if lastAction == Data:
            return
        speedleft = 1
        speedright = 1
        HBridge.setMotorLeft(speedleft)
        HBridge.setMotorRight(speedright)
        options[Data[1]](speedleft,speedright,Data)
        return

def midSpeed(Data):
        if lastAction == Data:
            return
        speedleft = 0.6
        speedright = 0.6
        HBridge.setMotorLeft(speedleft)
        HBridge.setMotorRight(speedright)
        options[Data[1]](speedleft,speedright,Data)
        return

def reverseSpeed(Data):
        if lastAction==Data:
            return

        speedleft = -0.6
        speedright = -0.6
        HBridge.setMotorLeft(speedleft)
        HBridge.setMotorRight(speedright)
        options[Data[1]](speedleft,speedright,Data)
        return

options = {
            '0' : noAction,
            '4' : turnRight,
            '6' :turnLeft,
            '5' : center,
            '3' : highSpeed,
            '2' : midSpeed,
            '1' : reverseSpeed,
}