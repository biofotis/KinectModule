#!/usr/bin/env python

import SocketServer
import qi
import stk.runner
import stk.events
import stk.services
import stk.logging
import threading
import time


class L2Kinect(object):

    APP_ID = "L2Kinect"
    def __init__(self, qiapp):
        self.qiapp = qiapp
        self.events = stk.events.EventHelper(qiapp.session) 
        self.s = stk.services.ServiceCache(qiapp.session) 
        self.logger = stk.logging.get_logger(qiapp.session, self.APP_ID)
        # Internal variables
        HOST, PORT = "localhost", 9999
        self.server = FotiosSocketServer((HOST, PORT), MyTCPHandler)
        server_t = threading.Thread(target=self.startserver)
        server_t.daemon = True
        server_t.start()
          
    def startserver(self):
        "start"
        self.server.serve_forever()

    @qi.bind(returnType=qi.Void, paramsType=[])
    def start(self):
        "starts"		
        server_t = threading.Thread(target=self.startserver)
        server_t.daemon = True
        server_t.start()
                
    def startcapture(self,participantID):
        "start capture"
        self.server.active_socket.send("START:" + str(participantID))

    def stopcapture(self):
        "stop capture"
        self.server.active_socket.send("STOP")	

    @qi.bind(returnType=qi.Void, paramsType=[])
    def stop(self):
        "Stop the service."
        self.server.shutdown()
        self.server.server_close()
        self.logger.info("Kinect Service stopped by user request.")
        self.qiapp.stop()
####################
# Setup and Run
####################

class FotiosSocketServer(SocketServer.TCPServer):


    def process_request(self, request, client_address):
        self.active_socket = request
        return SocketServer.TCPServer.process_request(self,request,client_address)

class MyTCPHandler(SocketServer.BaseRequestHandler):
       
    def handle(self):
        
        while True:
            self.data = self.request.recv(1024)
            if not self.data:
                break
            self.data = self.data.strip()
            print str(self.client_address[0]) + " wrote: "
            print self.data
            #self.request.send(self.data.upper())

if __name__ == "__main__":
    stk.runner.run_service(L2Kinect)
