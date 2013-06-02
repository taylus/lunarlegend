Tiled's map format is available from their github repository:
https://raw.github.com/bjorn/tiled/master/docs/map.xsd

It appears to be outdated, though... it's 2 years old as June 2012,
and it has elements that the TMX Map Format page says are deprecated in Tiled Qt (they were used in Tiled Java)
https://github.com/bjorn/tiled/wiki/TMX-Map-Format

So I edited the XSD manually to make it better match the TMX files saved by Tiled Qt.
These files won't include the xmlns, though, so we place it there before deserializing.

Generate the classes from it using Microsoft's xsd.exe:
http://msdn.microsoft.com/en-us/library/x6c1kb0s%28v=vs.80%29.aspx

xsd.exe map.xsd /classes /namespace:Tiled