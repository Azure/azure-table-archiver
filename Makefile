test:
	dotnet test -v n
check:
	dotnet test --no-build -v n
build:
	dotnet build
clean:
	rm -rf **/bin **/obj

all: build check
