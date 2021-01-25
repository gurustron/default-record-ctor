# Source generator for default record constructor

## Why?

Consider next code:
```c#
var firstName = "Foo";
var lastName = "Bar";
var person = new Person(lastName, firstName);
```
During review without IDE the error can easily slip through. Obviously you can use named parameters here:
```c#
var person = new Person(FirstName: firstName, LastName: lastName);
```
But IDE's quite often not big fans of those and also they can't be used in expression trees.

You can use initialization syntax, but it still will require passing parameters to constructor:
```c#
var person = new Person("", "")
{
    FirstName = firstName,
    LastName = lastName
};
```
## How does this work

Generator will create a default constructor for all **partial** records in solution which does not have one. 
Properties defined via record constructor syntax will be initialized to `default(T)` if no default value is specified. 

## What is supported:

- "Simple" records
- Generic records
- Nested records (declared in another class)
- Default parameters

## Plans

- Implement analyzer which will require all properties defined via record constructor syntax with no default value to be initialized via initialization syntax.
- Allow configure behaviour
- Your suggestions can be here... =)
