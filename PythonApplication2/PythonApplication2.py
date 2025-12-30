#Python Test mit Klassen und Asynchrone Funktionen

import tkinter as tk

class WriteFile:

    def __init__(self):
        print("Im Konstruktor")
        self.Verzeichnis=""
        self.Text=""

    def Schreiben(self, Verzeichnis, Text):
        Datei = open(Verzeichnis,'a')
        Datei.write(Text)


root = tk.Tk()
w = tk.Label(root, text="Hello Tkinter!")
w.pack()

print("Hier fängt's an")
referenz = WriteFile()
referenz.Schreiben('b:\\python_test\\testfile','Saublöder Text')
        

