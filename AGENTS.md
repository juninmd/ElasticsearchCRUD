```markdown
# AGENTS.md File Guidelines

These guidelines outline the principles and rules for development of the AGENTS.md repository. Adherence to these will ensure a maintainable, scalable, and high-quality codebase.

## 1. DRY (Don't Repeat Yourself)

*   **Single Responsibility Principle:** Each agent class should have a single, well-defined purpose.  Avoid creating multiple classes with similar functionality.
*   **Leverage Existing Code:**  When possible, reuse existing code patterns and components instead of reinventing the wheel.
*   **Abstraction:**  Define abstract interfaces for common agent functionalities and reuse those interfaces across different agents.

## 2. KISS (Keep It Simple, Stupid)

*   **Minimalism:** Strive for the simplest possible solution that meets the current requirements.  Avoid unnecessary complexity.
*   **Readability:** Code should be easy to understand, even for someone unfamiliar with the specific implementation. Use meaningful names, clear formatting, and comments when needed.
*   **Short Functions:** Keep functions and methods short and focused on a single task.

## 3. SOLID Principles

*   **Single Responsibility Principle (Again):** Ensure each class has a single reason to change.
*   **Open/Closed Principle:**  The system should be extensible without modifying the existing code. Implement new features through new classes, not by modifying existing ones.
*   **Liskov Substitution Principle:**  Subclasses should be substitutable for their base classes without affecting the correctness of the program.
*   **Interface Segregation Principle:** Clients should not be forced to depend on methods they don't use.
*   **Dependency Inversion Principle:**  High-level modules should not depend on low-level modules.  They should be dependent on abstractions.

## 4. YAGNI (You Aren't Gonna Need It)

*   **Avoid Premature Optimization:** Focus on implementing core functionality first.  Optimize only when performance becomes a bottleneck after the main logic is solid.
*   **Future-Proofing:**  Don't add features or complexity that isn't currently needed.  Refactor as needed to accommodate future requirements.

## 5. Code Style & Formatting

*   **Indentation:**  Use 2 spaces for indentation.  Don't use tabs.
*   **Line Length:**  Maximum 120 characters per line.
*   **Naming Conventions:**  Follow a consistent naming convention for classes, methods, and variables (e.g., CamelCase for classes, snake_case for variables).
*   **Comments:**  Provide concise and informative comments to explain complex logic or reasoning. Comments should *enhance* readability, not merely restate the code.

## 6. Test Coverage

*   **Unit Tests:** Aim for 80% test coverage.  Focus on testing individual functions and classes in isolation.
*   **Mocking:** Utilize mocks and stubs extensively during testing to isolate agent behavior and control external dependencies.  Avoid using real dependencies for testing purposes.
*   **Test-Driven Development:** Write tests *before* implementing the code.  This helps ensure that the code behaves as expected and that tests can be easily adapted for future changes.

## 7. File Size Limits

*   **Maximum File Size:** 180 lines of code.

## 8.  Code Structure & Organization

*   **Modular Design:** Divide the codebase into logical modules or components.
*   **Clear Separation of Concerns:** Each agent class should have a clearly defined purpose and responsibilities.
*   **Documentation:** Provide clear documentation for each agent class and module, including docstrings explaining their purpose and inputs/outputs.

## 9.  Development Process

*   **Code Reviews:** Conduct thorough code reviews to ensure adherence to these guidelines and to identify potential issues.
*   **Version Control:** Use Git for version control.  Establish a clear branching strategy.
*   **Continuous Integration:**  Implement a CI/CD pipeline to automate testing and deployment.

## 10. Specific Considerations

*   **Data Structures:** Carefully consider data structures for representing agents, data, and interactions.  Choose appropriate data structures based on performance requirements.
*   **Error Handling:** Implement robust error handling to gracefully handle unexpected situations.
*   **Logging:** Implement logging to track events and errors in the system.

These guidelines are intended to promote a consistently high-quality and well-organized codebase.  Regular review and updates are encouraged.
```