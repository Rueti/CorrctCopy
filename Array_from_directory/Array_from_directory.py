import os 
import time

os.chdir("b:\\Lorem ipsum\\")
#os.chdir("e:\\Fotos\\2020\\")



NumberOfFiles = (len(os.listdir(".")))
print (NumberOfFiles)
w, h = NumberOfFiles, NumberOfFiles
FL =[[0 for x in range(w)] for y in range(h)]
z = 0
OldestValueIndex = 9999999999 # That's Saturday, 20th. Nov. year 2286 06:46:39pm
#or 9'999'999'999 secs. after Thuersday, 1st. January 1970, 1:00am . This is used
#to find the oldest file from newest to the oldest.  :-)
CompareDate = 0
Index = 0
FilenameOfOldestFile = ""
CompleteFliePathOfOldestFile = ""



for root, dirs, files in os.walk('.', topdown= True):
    for name in files:
        Filename = (os.path.join(root, name))
        Filetimestamp = (os.path.getmtime(Filename))
        FL[z][0] = Filename
        FL[z][1] = Filetimestamp
        z=z+1

   
for i in range(0,z,1):
    print (FL[i][0])
    print("ist so alt:")
    CompareDate = (FL[i][1])
    if CompareDate < OldestValueIndex:
        OldestValueIndex = CompareDate
        Index = i
    print("und in Datumsschreibweise: ", time.ctime(FL[i][1]))
    print ('------------')
print("und der OldValueIndex ist: ", time.ctime(OldestValueIndex))
print ("und wird an ", Index, " Stelle gefunden")
FilenameOfOldestFile = (FL[Index][0])
print ("es handelt sich um die Datei ", FilenameOfOldestFile)
CompleteFliePathOfOldestFile = "e:\\Fotos\\2020\\" + FilenameOfOldestFile
print ("Der komplette Filepath: ", CompleteFliePathOfOldestFile )









        

        