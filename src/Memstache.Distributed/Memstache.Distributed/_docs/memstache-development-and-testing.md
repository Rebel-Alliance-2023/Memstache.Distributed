# MemStache.Distributed Development and Testing Guide

This guide is intended for developers who want to contribute to MemStache.Distributed or customize it for their own needs. It covers the project structure, how to set up a development environment, run tests, and contribute to the project.

## Project Structure

MemStache.Distributed is organized into several key directories and projects:

```
MemStache.Distributed/
├── src/
│   └── Memstache.Distributed/
│       ├── Compression/
│       ├── Encryption/
│       ├── EvictionPolicies/
│       ├── Factories/
│       ├── KeyVaultManagement/
│       ├── MultiTenancy/
│       ├── Performance/
│       ├── Providers/
│       ├── Resilience/
│       ├── Secure/
│       ├── Serialization/
│       ├── Telemetry/
│       └── Warmup/
├── tests/
│   └── Memstache.Distributed.Tests/
│       ├── Unit/
│       └── Integration/
├── samples/
│   └── Memstache.Distributed.Samples/
├── docs/
├── .github/
│   └── workflows/
└── README.md
```

- `src/Memstache.Distributed/`: Contains the main source code for the library.
- `tests/Memstache.Distributed.Tests/`: Contains unit and integration tests.
- `samples/`: Contains sample projects demonstrating usage of MemStache.Distributed.
- `docs/`: Contains documentation files.
- `.github/workflows/`: Contains GitHub Actions workflow files for CI/CD.

## Setting Up the Development Environment

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/MemStache.Distributed.git
   cd MemStache.Distributed
   ```

2. Install the .NET 8 SDK from https://dotnet.microsoft.com/download

3. Install required tools:
   ```
   dotnet tool install -g dotnet-format
   dotnet tool install -g dotnet-outdated
   ```

4. Restore dependencies:
   ```
   dotnet restore
   ```

5. Build the project:
   ```
   dotnet build
   ```

## Running Tests

MemStache.Distributed uses xUnit for unit and integration testing. To run the tests:

1. Navigate to the test project directory:
   ```
   cd tests/Memstache.Distributed.Tests
   ```

2. Run all tests:
   ```
   dotnet test
   ```

3. Run specific test categories:
   ```
   dotnet test --filter "Category=Unit"
   dotnet test --filter "Category=Integration"
   ```

4. Run tests with code coverage:
   ```
   dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
   ```

### Integration Tests

Integration tests require a running Redis instance and the Rebel Alliance KeyVault Secrets Emulator. You can use Docker to set these up:

1. Start Redis:
   ```
   docker run --name redis -p 6379:6379 -d redis
   ```

2. Start the Rebel Alliance KeyVault Secrets Emulator:
   ```
   docker run --name keyvault-emulator -p 5000:5000 -d rebelalliance/keyvault-secrets-emulator
   ```

Ensure these services are running before executing integration tests.

## Contributing Guidelines

We welcome contributions to MemStache.Distributed! Here are some guidelines to follow:

1. **Fork the Repository**: Create your own fork of the project.

2. **Create a Branch**: Create a new branch for your feature or bug fix.

3. **Follow Coding Standards**: 
   - Use C# coding conventions as outlined in the Microsoft docs.
   - Use meaningful names for variables, methods, and classes.
   - Write clear comments and documentation for public APIs.

4. **Write Tests**: Add or update tests for your changes. Aim for high test coverage.

5. **Run Code Analysis**: Use the built-in code analysis tools:
   ```
   dotnet format
   ```

6. **Update Documentation**: If your changes affect the public API or usage, update the relevant documentation.

7. **Create a Pull Request**: Submit a pull request with a clear title and description.

### Pull Request Process

1. Ensure your code adheres to the existing style of the project.
2. Update the README.md with details of changes to the interface, if applicable.
3. Increase the version numbers in any examples files and the README.md to the new version that this Pull Request would represent.
4. Your pull request will be reviewed by the maintainers. Be open to feedback and be prepared to make changes to your code.

### Reporting Issues

If you find a bug or have a suggestion for improvement:

1. Check if the issue already exists in the GitHub issue tracker.
2. If not, create a new issue, providing as much relevant information as possible.
3. If you're reporting a bug, include steps to reproduce the issue and any relevant error messages or logs.

## Continuous Integration

MemStache.Distributed uses GitHub Actions for continuous integration. The workflow is defined in `.github/workflows/ci.yml`. It runs on every push and pull request, performing the following steps:

1. Build the project
2. Run unit tests
3. Run integration tests
4. Generate code coverage report
5. Publish code coverage to Codecov

You can view the CI results in the GitHub Actions tab of the repository.

## Versioning

We use [Semantic Versioning](https://semver.org/) for versioning. For the versions available, see the tags on this repository.

## License

MemStache.Distributed is released under the MIT License. See the LICENSE file in the repository for full details.

By following these guidelines, you'll be able to set up your development environment, run tests, and contribute to MemStache.Distributed effectively. Remember to always write clean, well-documented code and accompany your changes with appropriate tests. Happy coding!

