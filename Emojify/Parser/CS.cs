using Emojify.EmojiData;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Emojify.Parser
{
    /// <summary>
    /// C# Parser
    /// </summary>
    public class CS : LanguageParser
    {
        private string dictionaryMapName;
        private string decoderBody = @"string __ccsc = null;
                System.Globalization.StringInfo meh = new System.Globalization.StringInfo(java);
                for (int i = 0; i < meh.LengthInTextElements; i++)
                {
                    string sjsjns = meh.SubstringByTextElements(i, 1);
                    foreach (var kvp in ____xxxxxxx)
                    {
                        if (kvp.Value == sjsjns)
                        {
                            __ccsc += kvp.Key.ToString();
                            continue;
                        }
                    }
                }
                return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(__ccsc));
            ";

        private string decoderMethodName;

        public CS() : base() {}

        /// <summary>
        /// Parse c# code
        /// </summary>
        /// <param name="inputFilePath">input file location</param>
        /// <param name="outputFilePath"output file location></param>
        public override void Parse(string inputFilePath, string outputFilePath)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[-] Reading {outputFilePath}");
            Console.ResetColor();
            string content = File.ReadAllText(inputFilePath);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[-] Parsing {outputFilePath}");
            Console.ResetColor();
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(content);
            SyntaxNode root = syntaxTree.GetRoot();

            // Create a Compilation with default references
            CSharpCompilation compilation = CSharpCompilation.Create(
                "DynamicAssembly",
                syntaxTrees: [syntaxTree]
            );
            // Get the SemanticModel for the SyntaxTree
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);

            dictionaryMapName =  GenerateName();
            decoderBody = decoderBody.Replace("____xxxxxxx", dictionaryMapName);

            root = RenameMethods(root, semanticModel);          
            root = RenameLocalVariables(root);
            root = RenameInterface(root);
            root = RenameClassAndConstructor(root);
            root = RenameGlobalVairables(root);
            root = AddStringDecoderMethod(root);
            root = EncodeStrings(root);
            root = EncodeCharacters(root);

            // Print the modified code
            string formattedCode = root.NormalizeWhitespace().ToFullString();
            WriteToFile(formattedCode, outputFilePath);
        }

        /// <summary>
        /// Obfuscate classes and constructors
        /// </summary>
        /// <param name="root">SyntaxNode</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RenameClassAndConstructor(SyntaxNode root)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[-] Obfuscating classes and constructors");
            Console.ResetColor();
            // Counter for renaming classes
            int classCounter = 0;
        
            // Visit the root node and rename classes and constructors
            var newRoot = root.ReplaceNodes(
                root.DescendantNodes().OfType<ClassDeclarationSyntax>(),
                (originalNode, _) =>
                {
                    // Rename the class
                    var newClassName = GenerateName();
                    var newClassNode = originalNode.WithIdentifier(SyntaxFactory.Identifier(newClassName));

                    // Rename the constructor(s) of the class
                    var newClassWithRenamedConstructor = newClassNode.ReplaceNodes(
                        newClassNode.DescendantNodes().OfType<ConstructorDeclarationSyntax>(),
                        (originalConstructorNode, _) =>
                        {
                            var newConstructorName = newClassName;
                            return originalConstructorNode.WithIdentifier(SyntaxFactory.Identifier(newConstructorName));
                        }
                    );

                    return newClassWithRenamedConstructor;
                }
            );

            // TODO rename class instantiations
            return newRoot;
        }

        /// <summary>
        /// Rename interfaces
        /// </summary>
        /// <param name="root">SyntaxNode</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RenameInterface(SyntaxNode root)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[-] Obfuscating interfaces");
            Console.ResetColor();
            // Find all interface declarations
            var interfaceDeclarations = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            // Dictionary to keep track of original interface names and their new names
            var interfaceNameMapping = new Dictionary<string, string>();
            
            foreach (var interfaceDeclaration in interfaceDeclarations)
            {
                // Rename the interface and store the mapping
                var newInterface = interfaceDeclaration.WithIdentifier(SyntaxFactory.Identifier(GenerateName()));
                interfaceNameMapping[interfaceDeclaration.Identifier.Text] = newInterface.Identifier.Text;

                // Replace the interface declaration with the renamed version
                root = root.ReplaceNode(interfaceDeclaration, newInterface);

                // Rename all references to the interface
                root = root.ReplaceNodes(
                    root.DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.Text == interfaceDeclaration.Identifier.Text),
                    (oldNode, _) => oldNode.WithIdentifier(SyntaxFactory.Identifier(interfaceNameMapping[interfaceDeclaration.Identifier.Text]))
                );
            }

            return root;
        }

        /// <summary>
        /// Rename methods and their invocations
        /// </summary>
        /// <param name="root">SyntaxNode</param>
        /// <param name="semanticModel">SemanticModel</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RenameMethods(SyntaxNode root, SemanticModel semanticModel)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[-] Obfuscating methods");
            Console.ResetColor();
            List<string> usedEmojiMethodNames = [];

            // Find all method declarations
            List<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();

            Dictionary<MethodDeclarationSyntax, MethodDeclarationSyntax> methodNameMap = methods.ToDictionary(
                m => m,
                m => 
                {
                    if (m.Identifier.Text.ToLower() == "main" ||
                        m.Parent is ClassDeclarationSyntax classDeclaration && m.Identifier.Text == classDeclaration.Identifier.Text)
                        return m;
                    
                    // Get the method symbol from the semantic model
                    var methodSymbol = semanticModel.GetDeclaredSymbol(m);

                    if (methodSymbol == null)
                    {
                        return m;
                    }

                    // Check if the method is implemented from an interface
                    var isFromInterface = methodSymbol.ContainingType.AllInterfaces
                        .SelectMany(i => i.GetMembers())
                        .OfType<IMethodSymbol>()
                        .Any(interfaceMethod => 
                            methodSymbol.Equals(methodSymbol.ContainingType.FindImplementationForInterfaceMember(interfaceMethod)));

                    // Check if the method is inherited from a base class
                    var isInherited = methodSymbol.IsOverride || methodSymbol.OverriddenMethod != null;
                    // Skip methods from an interface or inherited from a base class
                    if (isFromInterface || isInherited)
                    {
                        return m;
                    }

                    return m.WithIdentifier(SyntaxFactory.Identifier(GenerateName()));
                }
            );

            // Replace method declarations in the syntax tree
            var newroot = root.ReplaceNodes(methods, (oldNode, newNode) => 
                methodNameMap[oldNode]);

            // Collect all invocation expressions in the syntax tree
            var invocationExpressions = newroot.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();

            // Update method invocations
            var updatedroot = newroot.ReplaceNodes(
                invocationExpressions,
                (oldNode, newNode) =>
            {
                // Check if the invoked method is a simple identifier (e.g., methodName())
                if (oldNode.Expression is IdentifierNameSyntax identifierName)
                {
                    var oldMethodName = identifierName.Identifier.Text;

                    // Check if the old method name exists in the renaming map
                    var matchingMethod = methodNameMap.Keys.FirstOrDefault(m => m.Identifier.Text == oldMethodName);
                    if (matchingMethod != null)
                    {
                        // Replace the method name with the new one
                        var newMethodName = methodNameMap[matchingMethod].Identifier.Text;
                        var newIdentifier = SyntaxFactory.IdentifierName(newMethodName);

                        // Return the updated invocation expression
                        return oldNode.WithExpression(newIdentifier);
                    }
                }

                // If no changes are needed, return the original node
                return oldNode;
            });

            return updatedroot;
        }

        /// <summary>
        /// Rename global variables its refernces
        /// </summary>
        /// <param name="root">SyntaxNode</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RenameGlobalVairables(SyntaxNode root)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[-] Obfuscating global variables");
            Console.ResetColor();
            // List to track used variable names to avoid conflicts
            HashSet<string> usedGlobalVarNames = new HashSet<string>();

            // Find all global variable declarations (fields in classes)
            var globalVariables = root.DescendantNodes().OfType<FieldDeclarationSyntax>().ToList();

            // Map original variable declarations to new variable names
            var globalVarNameMap = globalVariables.ToDictionary(
                v => v,
                v =>
                {
                    // Generate a new name for the variable
                    var newVarName = GenerateName();
                    usedGlobalVarNames.Add(newVarName);
                    return v.WithDeclaration(v.Declaration.WithVariables(
                        SyntaxFactory.SingletonSeparatedList(v.Declaration.Variables.First().WithIdentifier(SyntaxFactory.Identifier(newVarName)))
                    ));
                }
            );

            // Replace global variable declarations in the syntax tree
            var newRoot = root.ReplaceNodes(globalVariables, (oldNode, newNode) => globalVarNameMap[oldNode]);;
            // Replace usages of global variables
            var allIdentifiers = newRoot.DescendantNodes().OfType<IdentifierNameSyntax>().ToList();
            newRoot = newRoot.ReplaceNodes(allIdentifiers, (oldNode, newNode) =>
            {
                var oldVarName = oldNode.Identifier.Text;

                // Check if the old variable name exists in the renaming map for global variables
                var matchingVarDecl = globalVarNameMap.Keys
                    .FirstOrDefault(d => d.Declaration.Variables.First().Identifier.Text == oldVarName);

                if (matchingVarDecl != null)
                {
                    var newVarName = globalVarNameMap[matchingVarDecl].Declaration.Variables.First().Identifier.Text;
                    return oldNode.WithIdentifier(SyntaxFactory.Identifier(newVarName));
                }

                // Return the original node if no change is needed
                return oldNode;
            });

            return newRoot;
        }

        /// <summary>
        /// Rename local variables and its references
        /// </summary>
        /// <param name="root">SyntaxNode</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RenameLocalVariables(SyntaxNode root)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[-] Obfuscating local variables");
            Console.ResetColor();
            // List to track used variable names to avoid conflicts
            List<string> usedLocalVarNames = new List<string>();

            // Find all local variable declarations
            var localVariables = root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>().ToList();

            // Map original variable declarations to new variable names
            var localVarNameMap = localVariables.ToDictionary(
                v => v,
                v =>
                {
                    // Only rename if not a 'main' method or reserved keyword
                    var newVarName = GenerateName();
                    usedLocalVarNames.Add(newVarName);
                    return v.WithDeclaration(v.Declaration.WithVariables(
                        SyntaxFactory.SingletonSeparatedList(v.Declaration.Variables.First().WithIdentifier(SyntaxFactory.Identifier(newVarName)))
                    ));
                }
            );

            // Replace local variable declarations in the syntax tree
            var newRoot = root.ReplaceNodes(localVariables, (oldNode, newNode) => localVarNameMap[oldNode]);

            // Now, for each local variable declaration, replace its usages only inside its scope.
            foreach (var variableDeclaration in localVarNameMap.Keys)
            {
                var newVarName = localVarNameMap[variableDeclaration].Declaration.Variables.First().Identifier.Text;

                // Find all identifier usages that are within the scope of this variable's block
                var blockScope = GetParentBlock(variableDeclaration);
                if (blockScope != null)
                {
                    var allIdentifiersInScope = blockScope.DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.Text == variableDeclaration.Declaration.Variables.First().Identifier.Text).ToList();

                    // Replace usages of local variables within the block scope
                    newRoot = newRoot.ReplaceNodes(allIdentifiersInScope, (oldNode, newNode) =>
                    {
                        return oldNode.WithIdentifier(SyntaxFactory.Identifier(newVarName));
                    });
                }
            }

            return newRoot;
        }

        // Helper function to get the parent block of a node (local variable declaration)
        private SyntaxNode GetParentBlock(SyntaxNode node)
        {
            // Look for method, loop, or block syntax that contains the variable declaration
            var parentMethod = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (parentMethod != null)
            {
                return parentMethod.Body; // The body of the method is the scope for local variables
            }

            var parentIf = node.Ancestors().OfType<IfStatementSyntax>().FirstOrDefault();
            if (parentIf != null)
            {
                return parentIf.Statement; // If the variable is inside an 'if' block
            }

            var parentLoop = node.Ancestors().OfType<ForStatementSyntax>().FirstOrDefault();
            if (parentLoop != null)
            {
                return parentLoop.Statement; // If the variable is inside a 'for' loop
            }

            var parentWhile = node.Ancestors().OfType<WhileStatementSyntax>().FirstOrDefault();
            if (parentWhile != null)
            {
                return parentWhile.Statement; // If the variable is inside a 'while' loop
            }

            return null; // Return null if no block is found
        }

        /// <summary>
        /// Obfuscate string literals using emojis
        /// </summary>
        /// <param name="root">SyntaxNode</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode EncodeStrings(SyntaxNode root)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[-] Obfuscating string literals");
            Console.ResetColor();
            // Traverse the tree to find string literals inside methods
            var newRoot = root.ReplaceNodes(
            root.DescendantNodes().OfType<MethodDeclarationSyntax>(),
            (originalMethod, _) =>
            {
                if (originalMethod.ParameterList.Parameters
                    .Any(param =>
                        param.Type is PredefinedTypeSyntax predefinedType &&
                        predefinedType.Keyword.IsKind(SyntaxKind.StringKeyword) &&
                        param.Identifier.Text == "java"))
                {
                    return originalMethod;
                }

                // Modify each method to replace string literals
                var updatedMethod = originalMethod.ReplaceNodes(
                    originalMethod.DescendantNodes().OfType<LiteralExpressionSyntax>()
                    .Where(literal => literal.IsKind(SyntaxKind.StringLiteralExpression)),
                    (originalLiteral, _) =>
                    {
                        var uniqueArgumentValue = EmojiEncodeString(originalLiteral.Token.ValueText); 

                        // Create the method call node
                        var argument = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(uniqueArgumentValue)));
                        
                        // Replace the string literal with GetString("literal")
                        return SyntaxFactory.InvocationExpression(
                                SyntaxFactory.IdentifierName(decoderMethodName))
                            .WithArgumentList(
                                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([argument])));
                    });

                return updatedMethod;
            });

             // Replace all string literals with a method call
            return newRoot;
        }

        /// <summary>
        /// Obfuscate character literals using emojis
        /// </summary>
        /// <param name="root">SyntaxNode</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode EncodeCharacters(SyntaxNode root)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[-] Obfuscating ccharacter literals");
            Console.ResetColor();
            // Traverse the tree to find string literals inside methods
            var newRoot = root.ReplaceNodes(
            root.DescendantNodes().OfType<MethodDeclarationSyntax>(),
            (originalMethod, _) =>
            {
                // Skip decoder method
                if (originalMethod.ParameterList.Parameters
                    .Any(param =>
                        param.Type is PredefinedTypeSyntax predefinedType &&
                        predefinedType.Keyword.IsKind(SyntaxKind.StringKeyword) &&
                        param.Identifier.Text == "java"))
                {
                    return originalMethod;
                }

                // Modify each method to replace character literals
                var updatedMethod = originalMethod.ReplaceNodes(
                    originalMethod.DescendantNodes().OfType<LiteralExpressionSyntax>()
                    .Where(literal => literal.IsKind(SyntaxKind.CharacterLiteralExpression)),
                    (originalLiteral, _) =>
                    {
                        var uniqueArgumentValue = EmojiEncodeString(originalLiteral.Token.ValueText); 

                        // Create the method call node
                        var argument = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(uniqueArgumentValue)));
                        
                        // Replace the character literal
                        return SyntaxFactory.ElementAccessExpression(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.IdentifierName(decoderMethodName))
                            .WithArgumentList(
                                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[] { argument }))))
                        .WithArgumentList(
                            SyntaxFactory.BracketedArgumentList(
                                SyntaxFactory.SeparatedList(new[] {
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))
                                })));
                    });

                return updatedMethod;
            });

             // Replace all string literals with a method call
            return newRoot;
        }

        /// <summary>
        /// Add string decoder method to every class
        /// </summary>
        /// <param name="root">SyntaxNode</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode AddStringDecoderMethod(SyntaxNode root)
        {
            decoderMethodName = GenerateName();
            // Create a new method
            var parameters = new[]
            {
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("java"))
                    .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))),
            };
            // Parse the method body into a BlockSyntax
            var methodBody = SyntaxFactory.Block(SyntaxFactory.ParseStatement($"{{ {decoderBody} }}"));
            var newMethod = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                SyntaxFactory.Identifier(decoderMethodName))
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                    .AddParameterListParameters(parameters)
                    .WithBody(methodBody);

            // Find all class declarations in the code
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            // Iterate through each class declaration and modify it
            var updatedClasses = new List<MemberDeclarationSyntax>();
            foreach (var classDeclaration in classDeclarations)
            {
                var dictionaryField = CreateDictionaryField(EmojiCharacterMap);
                // Add the new method to the class
                var newClass = classDeclaration.AddMembers(newMethod, dictionaryField);

                updatedClasses.Add(newClass);
            }

            return root.ReplaceNodes(classDeclarations, (oldNode, _) => updatedClasses.FirstOrDefault());;
        }

        /// <summary>
        /// Helper method to create a dictionary field
        /// </summary>
        /// <param name="dictionaryData">Dictionary's content</param>
        /// <returns>Dictionary<string, string></returns>
        private FieldDeclarationSyntax CreateDictionaryField(Dictionary<string, string> dictionaryData)
        {
            // Create the dictionary initialization (key-value pairs)
            var dictionaryInitialization = SyntaxFactory.InitializerExpression(
                SyntaxKind.CollectionInitializerExpression,
                SyntaxFactory.SeparatedList<ExpressionSyntax>(
                    dictionaryData.Select(kv =>
                        SyntaxFactory.InitializerExpression(
                            SyntaxKind.ComplexElementInitializerExpression,
                            SyntaxFactory.SeparatedList<ExpressionSyntax>(
                                new[]
                                {
                                    SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(kv.Key)),
                                    SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(kv.Value))
                                }
                            )
                        )
                    )
                )
            );

            // Create the dictionary field declaration
            var dictionaryType = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("Dictionary"))
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList<TypeSyntax>(
                        new[] { SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)) }
                    )
                ));

            // Add the `new` keyword explicitly for dictionary initialization
            var dictionaryField = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(dictionaryType)
                    .AddVariables(SyntaxFactory.VariableDeclarator(dictionaryMapName)
                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.GenericName(SyntaxFactory.Identifier("Dictionary"))
                                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                                        SyntaxFactory.SeparatedList<TypeSyntax>(
                                            new[] {
                                                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                                                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))
                                            }
                                        )
                                    ))
                            )
                            .WithArgumentList(SyntaxFactory.ArgumentList())
                            .WithInitializer(dictionaryInitialization)
                        ))
                    ))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword));


            return dictionaryField;
        }

        /// <summary>
        /// Convert string to emoji
        /// </summary>
        /// <param name="str">String to be converted</param>
        /// <returns>Emojis</returns>
        private string EmojiEncodeString(string str)
        {
            string base64encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(str));
            string newstr = "";
            foreach (char ch in base64encoded)
            {
                newstr += EmojiCharacterMap[ch.ToString()];
            }

            return newstr;
        }
    }
}