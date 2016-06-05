# Vorrennung
Die Vorrennung dient zur Beschleunigung von Videos mit Sprache. Sprechpausen werden
dabei wesentlich schneller beschleunigt, was Zeit sparen soll.

Vorbereitung:
Die Vorrennung benötigt FFMPEG.
Beim ersten Start muss man den bin-Ordner der FFMPEG-Installation in das Programm draggen,
damit die Pfade entsprechend gesetzt werden können.

Verwendung:

Eine Datei beschleunigen:
Um eine Datei zu beschleunigen muss man diese vorerst öffnen (via Datei Öffnen oder indem man die Datei in das Programm draggt).
Sobald die Datei verarbeitet ist geht es an die Einstellungen.

Für jede Datei sollte die Einstellung "Schwellwert Stille" und "Schwellwert Sprache" angepasst werden.
Der Knopf "Autokalibrieren" erledigt dies automatisch, eine manuelle Einstellung wird aber empfohlen.
"Schwellwert Stille" entspricht der Lautstärke, unterhalb welcher die Beschleunigung erhöht wird.
"Schwellwert Sprache" entspricht der Lautstärke, überhalb welcher die Beschleunigung auf die Grundbeschleunigung zurückgesetzt wird.
Es wird empfohlen, unter Sonstiges die Infografiken zu öffnen um zu sehen, wie sich die Einstellungen auf die Beschleunigung auswirken.
Sobald man die Parameter wie gewünscht eingestellt hat kann man das Video beschleunigen, entweder via "Datei->Beschleunigen->Video"
"Generiere->Video".

Zu den Infografiken:
Die erste Grafik zeigt den Lautstärkeverlauf der Datei.
Die zweite Grafik zeigt die Menge an Zeit, die die entsprechende Stelle im Output einnehmen wird. (entspricht 1/Beschleunigungsfaktor)
Die dritte Grafik zeigt die Lautstärkeverteilung. Links entspricht leise, rechts entspricht laut.

Zu den Einstellungen:
Grundbeschleunigung:           Wie schnell Sprache beschleunigt wird.
Maximale Beschleunigung:       Wie schnell Stille maximal beschleunigt wird.
Minimale Stillebeschleunigung: Wie schnell Stille minimal beschleunigt wird.
Schwellwert Stille:            Bis zu welcher Lautstärke Audio als Still erkannt wird.
Schwellwert Sprache:           Ab welcher Lautstärke Audio als Sprache erkannt wird.
Reaktionsgeschwindigkeit:      Wie schnell die Beschleunigung hochgeregelt wird.
Rückwärtsprüfung:              Wenn diese Option angehakt ist, dann wird die Beschleunigung nach einer Stille langsam wieder hochgeregelt. Wenn Wortanfänge abgeschnitten werden, ist diese Option zu empfehlen.
Ableitung berücksichtigen:     Experimentell, dafür gedacht, leiser werdende Sprache noch als diese zu erkennen. Dazu gehört maximaler Abfall, minimaler Abfall und Ableitungsglättung.
Eigene FPS:                    Wenn diese Option angehakt ist, dann hat das Ergebnisvideo die daneben spezifizierte Framerate.
SolaBlockDiv:                  Die Blockgröße für den Solaalgorithmus. Höhere Werte entsprechen kleineren Blöcken.
SolaSuchBerDiv:                Wie groß der Bereich ist, in dem der Algorithmus nach passenden Blöcken sucht. Beinflusst die Konvertierungszeit und die Audioqualität. Höhere Werte entsprechen kleineren Blöcken.
SolaSuchGenauigkeit:           Wie genau Sola nach optimalen Audioübergängen sucht. Höhere Werte benötigen mehr Rechenzeit, ergeben aber eine bessere Audioqualität.

Sonstiges->Generiere Dragdrop  Wenn diese Option angehakt ist, dann wird nicht nach einer Zieldatei gefragt, sondern eine temporäre Datei erstellt, die man dann aus diesem Programm rausdraggen kann.
Sonstiges->Simultanbeschl.     Wenn diese Option angehakt ist, dann wird beim Öffnen einer zweiten Datei gefragt, ob diese exakt wie die vorherige Datei beschleunigt werden soll. Das überschreibt die Audiospur und dient der Beschleunigung zweier zusammengehöriger Videos.
Sonstiges->Dynaudnorm          Ob die Lautstärkeverteilung geglättet werden soll.

Zu beachtendes:
Die Verwendung dieses Programms geschieht auf eigene Gefahr, ich übernehme keine Haftung für Schäden, die durch die Nutzung
dieses Programmes entstehen könnten.

Das Programm legt einen temporären Ordner in dem Arbeitsverzeichnis an. Wenn dieser nicht automatisch gelöscht wird, dann kann er
auch manuell gelöscht werden, solange das Programm nicht gestartet ist.
