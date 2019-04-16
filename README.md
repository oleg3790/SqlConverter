## Overview

Library which exposes a service that will convert (a specific DB's) SQL DML (update, insert and merge) into selects and partially validate parameters

## Usage

Instantiate a converter service object, check validity of SQL; and invoke the convert call.

```c#
var converter = new SqlConverterService(SQL, LogicalId);

if (converter.IsSqlValid())
{
    string convertedSQL = converter.Convert();
}               
else
{
    string status = converter.GetValidationErrorMessage();
}     
```
