# geoservices

GeoServices is a C# .NET library written to facilitate communication between consumers and ESRI ArcObjects SDE.

## Usage

1. Clone this repository
2. Build and include this library from the cloned source in your .NET project
3. Clone, build and include the <a href="https://www.github.com/juwilliams/spatial-connect">SpatialConnect.Entity</a> repository which GeoServices depends on for the Container class.
4. Initialize a DataRetrievalManager
5. Provide the DataRetrievalManager with an instance of a Container during construction
6. Attach event handlers for the OnDataRetrievalError and OnDataRetrievalSuccess events fired by the DataRetrievalManager instance
7. Call GetData() on the DataRetrievalManager instance

For an example of the above reference <a href="https://github.com/juwilliams/spatial-connect/blob/master/SpatialConnect.Windows.DataServices/Service/RecordCaptureService.cs">this</a> file.