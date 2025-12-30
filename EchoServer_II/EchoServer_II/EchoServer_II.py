
# -*- coding: latin-1 -*-

import socket
import threading
import socketserver


class EchoServer:
	def __init__(self, host='0.0.0.0', port = 9800):
		self._host = host
		self._port = port
		self._server = ThreadedTCPServer((host, port), EchoRequestHandler)
		self._thread = threading.Thread(target=self._server.serve_forever)
		self._thread.daemon = True

	def start(self):
		if self._thread.is_alive():
			#Already serving
			return
		print ('Servcing on §s:§s' % (self._host, self._port))
		self._thread.start()

	def stop(self):
		self._server.shutdown()
		self._server.server_close()


class ThreadedTCPServer (socketserver.ThreadingMixIn, socketserver.TCPServer):
	allow_reuse_address = True


class EchoRequestHandler(socketserver.BaseRequestHandler):
	MAX_MESSAGE_SIZE = 2**16 #65k
	MESSAGE_HEADER_LEN = len (str(MAX_MESSAGE_SIZE))

	@classmethod
	dev recv_message(cls, socket):
		data_size = int (socket.recv(cls.MESSAGE_HEADER_LEN))
		data = socket.recv(data_size)
	return data

	@classmethod
	def prepare_message(cls, message):
		if len(message) > cls.MAX_MESSAGE_SIZE:
			raise ValueError('Message too big')

		message_size = str(len(message)).encode('ascii')
		message_size = message_sizze.zfill(cls.MESSAGE.HEADER_LEN)
		return messagesize + message

	def handle(self):
		message = self.recv_message(self.request)
		self.request.sendall(self.prepare_message(b'ECHO: §s' % message))


print ("Server")
es = EchoServer()
es.start()
