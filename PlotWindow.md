<Window x:Class="FloridaLotteryApp.PlotWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:FloridaLotteryApp"
        Title="Análisis de Plot"
        Height="800" 
        Width="1200"
        WindowStartupLocation="CenterOwner"
        Background="#F0F0F0">
    
    <Window.Resources>
        <Style TargetType="Border" x:Key="DigitBorder">
            <Setter Property="Width" Value="45"/>
            <Setter Property="Height" Value="45"/>
            <Setter Property="CornerRadius" Value="22.5"/>
            <Setter Property="BorderThickness" Value="2"/>
            <Setter Property="BorderBrush" Value="#333333"/>
            <Setter Property="Margin" Value="3,0"/>
            <Setter Property="Background" Value="White"/>
        </Style>
        
        <Style TargetType="TextBlock" x:Key="DigitText">
            <Setter Property="FontSize" Value="20"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="HorizontalAlignment" Value="Center"/>
            <Setter Property="VerticalAlignment" Value="Center"/>
        </Style>
        
        <Style TargetType="Border" x:Key="FireballBorder">
            <Setter Property="Width" Value="35"/>
            <Setter Property="Height" Value="35"/>
            <Setter Property="CornerRadius" Value="17.5"/>
            <Setter Property="BorderThickness" Value="2"/>
            <Setter Property="BorderBrush" Value="#FF5722"/>
            <Setter Property="Margin" Value="3,0"/>
            <Setter Property="Background" Value="#FFE0B2"/>
        </Style>
    </Window.Resources>

    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="20"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Tabla superior -->
        <Border Grid.Row="0" 
                Background="White" 
                CornerRadius="10" 
                Padding="15"
                BorderThickness="1" 
                BorderBrush="#CCCCCC">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <!-- Encabezados -->
                <Grid Grid.Row="0" Margin="0,0,0,10">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    
                    <TextBlock Grid.Column="0" 
                               Text="Patrón de referencia" 
                               FontWeight="Bold" 
                               FontSize="14" 
                               HorizontalAlignment="Center"
                               Padding="5"/>
                    <TextBlock Grid.Column="1" 
                               Text="Coincidencias" 
                               FontWeight="Bold" 
                               FontSize="14" 
                               HorizontalAlignment="Center"
                               Padding="5"/>
                    <TextBlock Grid.Column="2" 
                               Text="Patrones similares" 
                               FontWeight="Bold" 
                               FontSize="14" 
                               HorizontalAlignment="Center"
                               Padding="5"/>
                    <TextBlock Grid.Column="3" 
                               Text="Coincidencias" 
                               FontWeight="Bold" 
                               FontSize="14" 
                               HorizontalAlignment="Center"
                               Padding="5"/>
                </Grid>

                <!-- ScrollViewer para la tabla -->
                <ScrollViewer Grid.Row="1" 
                              MaxHeight="300" 
                              VerticalScrollBarVisibility="Auto"
                              HorizontalScrollBarVisibility="Auto">
                    <ItemsControl x:Name="PatternsTable">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <Grid Margin="0,3,0,3">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="*"/>
                                        <ColumnDefinition Width="*"/>
                                    </Grid.ColumnDefinitions>
                                    
                                    <!-- Patrón de referencia -->
                                    <Border Grid.Column="0" 
                                            Background="#E3F2FD" 
                                            Padding="8,4" 
                                            Margin="2"
                                            CornerRadius="4"
                                            BorderThickness="1"
                                            BorderBrush="#90CAF9">
                                        <TextBlock Text="{Binding ReferenceNumber}" 
                                                   FontWeight="Bold" 
                                                   HorizontalAlignment="Center"
                                                   FontSize="14"
                                                   FontFamily="Consolas"/>
                                    </Border>
                                    
                                    <!-- Coincidencias del patrón de referencia -->
                                    <Border Grid.Column="1" 
                                            Background="#F5F5F5" 
                                            Padding="8,4" 
                                            Margin="2"
                                            CornerRadius="4"
                                            BorderThickness="1"
                                            BorderBrush="#CCCCCC">
                                        <TextBlock Text="{Binding MatchNumber}" 
                                                   FontWeight="Bold" 
                                                   HorizontalAlignment="Center"
                                                   FontSize="14"
                                                   FontFamily="Consolas"/>
                                    </Border>
                                    
                                    <!-- Patrones similares -->
                                    <Border Grid.Column="2" 
                                            Background="#E8F5E8" 
                                            Padding="8,4" 
                                            Margin="2"
                                            CornerRadius="4"
                                            BorderThickness="1"
                                            BorderBrush="#A5D6A5">
                                        <TextBlock Text="{Binding SimilarPatternNumber}" 
                                                   FontWeight="Bold" 
                                                   HorizontalAlignment="Center"
                                                   FontSize="14"
                                                   FontFamily="Consolas"/>
                                    </Border>
                                    
                                    <!-- Coincidencias de patrones similares -->
                                    <Border Grid.Column="3" 
                                            Background="#F5F5F5" 
                                            Padding="8,4" 
                                            Margin="2"
                                            CornerRadius="4"
                                            BorderThickness="1"
                                            BorderBrush="#CCCCCC">
                                        <TextBlock Text="{Binding SimilarMatchNumber}" 
                                                   FontWeight="Bold" 
                                                   HorizontalAlignment="Center"
                                                   FontSize="14"
                                                   FontFamily="Consolas"/>
                                    </Border>
                                </Grid>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </ScrollViewer>
            </Grid>
        </Border>

        <!-- Sección inferior -->
        <StackPanel Grid.Row="2" Background="White" CornerRadius="10" Padding="15" BorderThickness="1" BorderBrush="#CCCCCC">
            
            <!-- Fila 1: Tirada seleccionada (Pick3 y Pick4) -->
            <Border Background="#FFF3E0" CornerRadius="8" Padding="10" Margin="0,0,0,10">
                <StackPanel>
                    <TextBlock Text="TIRADA SELECCIONADA" FontWeight="Bold" FontSize="12" Foreground="#FF9800" Margin="0,0,0,8"/>
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        
                        <!-- Pick 3 seleccionado -->
                        <StackPanel Grid.Column="0" Orientation="Horizontal" Margin="0,0,40,0">
                            <TextBlock Text="Pick 3:" FontWeight="Bold" VerticalAlignment="Center" Margin="0,0,10,0"/>
                            <ItemsControl x:Name="SelectedPick3Digits" ItemsSource="{Binding SelectedPick3}">
                                <ItemsControl.ItemsPanel>
                                    <ItemsPanelTemplate>
                                        <StackPanel Orientation="Horizontal"/>
                                    </ItemsPanelTemplate>
                                </ItemsControl.ItemsPanel>
                                <ItemsControl.ItemTemplate>
                                    <DataTemplate>
                                        <Border Style="{StaticResource DigitBorder}">
                                            <TextBlock Style="{StaticResource DigitText}" Text="{Binding}"/>
                                        </Border>
                                    </DataTemplate>
                                </ItemsControl.ItemTemplate>
                            </ItemsControl>
                        </StackPanel>
                        
                        <!-- Pick 4 seleccionado -->
                        <StackPanel Grid.Column="1" Orientation="Horizontal" Margin="0,0,40,0">
                            <TextBlock Text="Pick 4:" FontWeight="Bold" VerticalAlignment="Center" Margin="0,0,10,0"/>
                            <ItemsControl x:Name="SelectedPick4Digits" ItemsSource="{Binding SelectedPick4}">
                                <ItemsControl.ItemsPanel>
                                    <ItemsPanelTemplate>
                                        <StackPanel Orientation="Horizontal"/>
                                    </ItemsPanelTemplate>
                                </ItemsControl.ItemsPanel>
                                <ItemsControl.ItemTemplate>
                                    <DataTemplate>
                                        <Border Style="{StaticResource DigitBorder}">
                                            <TextBlock Style="{StaticResource DigitText}" Text="{Binding}"/>
                                        </Border>
                                    </DataTemplate>
                                </ItemsControl.ItemTemplate>
                            </ItemsControl>
                        </StackPanel>
                        
                        <!-- Fecha y tirada -->
                        <StackPanel Grid.Column="2" Orientation="Horizontal">
                            <TextBlock Text="Fecha:" FontWeight="Bold" VerticalAlignment="Center" Margin="0,0,5,0"/>
                            <TextBlock x:Name="SelectedDate" VerticalAlignment="Center" Margin="0,0,20,0"/>
                            <TextBlock x:Name="SelectedDrawIcon" FontSize="20" VerticalAlignment="Center"/>
                        </StackPanel>
                    </Grid>
                </StackPanel>
            </Border>
            
            <!-- Fila 2: Dígitos del 0-9 con los repetidos marcados -->
            <Border Background="#F5F5F5" CornerRadius="8" Padding="10" Margin="0,0,0,10">
                <StackPanel>
                    <TextBlock Text="DÍGITOS DISPONIBLES" FontWeight="Bold" FontSize="12" Foreground="#666666" Margin="0,0,0,8"/>
                    <WrapPanel>
                        <ItemsControl x:Name="DigitsPanel" ItemsSource="{Binding Digits}">
                            <ItemsControl.ItemsPanel>
                                <ItemsPanelTemplate>
                                    <WrapPanel/>
                                </ItemsPanelTemplate>
                            </ItemsControl.ItemsPanel>
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <Border Style="{StaticResource DigitBorder}" 
                                            Background="{Binding Background}" 
                                            BorderBrush="{Binding BorderColor}"
                                            Margin="3">
                                        <TextBlock Style="{StaticResource DigitText}" 
                                                   Text="{Binding Digit}"
                                                   Foreground="{Binding TextColor}"/>
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </WrapPanel>
                </StackPanel>
            </Border>
            
            <!-- Fila 3: Probabilidades -->
            <Border Background="#F5F5F5" CornerRadius="8" Padding="10" Margin="0,0,0,10">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    
                    <StackPanel Grid.Column="0" Orientation="Horizontal" HorizontalAlignment="Center">
                        <TextBlock Text="Probabilidad Pick 3:" FontWeight="Bold" Margin="0,0,10,0"/>
                        <TextBlock x:Name="Pick3Probability" Text="0%" Foreground="#2196F3" FontWeight="Bold"/>
                    </StackPanel>
                    
                    <StackPanel Grid.Column="1" Orientation="Horizontal" HorizontalAlignment="Center">
                        <TextBlock Text="Probabilidad Pick 4:" FontWeight="Bold" Margin="0,0,10,0"/>
                        <TextBlock x:Name="Pick4Probability" Text="0%" Foreground="#4CAF50" FontWeight="Bold"/>
                    </StackPanel>
                </Grid>
            </Border>
            
            <!-- Fila 4: Números sugeridos -->
            <Border Background="#F5F5F5" CornerRadius="8" Padding="10">
                <StackPanel>
                    <TextBlock Text="NÚMEROS SUGERIDOS" FontWeight="Bold" FontSize="12" Foreground="#666666" Margin="0,0,0,8"/>
                    
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        
                        <!-- Pick 3 sugerido -->
                        <TextBlock Grid.Row="0" Grid.Column="0" Text="Pick 3:" FontWeight="Bold" VerticalAlignment="Center" Margin="0,0,10,10"/>
                        <StackPanel Grid.Row="0" Grid.Column="1" Orientation="Horizontal" Margin="0,0,0,10">
                            <ItemsControl x:Name="SuggestedPick3Digits" ItemsSource="{Binding SuggestedPick3}">
                                <ItemsControl.ItemsPanel>
                                    <ItemsPanelTemplate>
                                        <StackPanel Orientation="Horizontal"/>
                                    </ItemsPanelTemplate>
                                </ItemsControl.ItemsPanel>
                                <ItemsControl.ItemTemplate>
                                    <DataTemplate>
                                        <Border Style="{StaticResource DigitBorder}" Background="#E8F5E8">
                                            <TextBlock Style="{StaticResource DigitText}" Text="{Binding}"/>
                                        </Border>
                                    </DataTemplate>
                                </ItemsControl.ItemTemplate>
                            </ItemsControl>
                        </StackPanel>
                        
                        <!-- Pick 4 sugerido -->
                        <TextBlock Grid.Row="1" Grid.Column="0" Text="Pick 4:" FontWeight="Bold" VerticalAlignment="Center" Margin="0,0,10,0"/>
                        <StackPanel Grid.Row="1" Grid.Column="1" Orientation="Horizontal">
                            <ItemsControl x:Name="SuggestedPick4Digits" ItemsSource="{Binding SuggestedPick4}">
                                <ItemsControl.ItemsPanel>
                                    <ItemsPanelTemplate>
                                        <StackPanel Orientation="Horizontal"/>
                                    </ItemsPanelTemplate>
                                </ItemsControl.ItemsPanel>
                                <ItemsControl.ItemTemplate>
                                    <DataTemplate>
                                        <Border Style="{StaticResource DigitBorder}" Background="#E3F2FD">
                                            <TextBlock Style="{StaticResource DigitText}" Text="{Binding}"/>
                                        </Border>
                                    </DataTemplate>
                                </ItemsControl.ItemTemplate>
                            </ItemsControl>
                        </StackPanel>
                    </Grid>
                </StackPanel>
            </Border>
        </StackPanel>
    </Grid>
</Window>